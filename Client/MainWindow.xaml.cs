using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private byte[] buffer = new byte[1024];
        private IPEndPoint? endpoint;
        private DateTime lastSyncMoment;
        private Random random;

        public MainWindow()
        {
            InitializeComponent();
            lastSyncMoment = DateTime.MinValue;
            random = new();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            authorTextBox.Text = "User " + random.Next(1, 100);
            ReCheckMessages();
        }

        private IPEndPoint? InitEndpoint()
        {
            if (endpoint is not null) return endpoint;
            try
            {
                IPAddress ip =               // На окне IP - это текст ("127.0.0.1")
                    IPAddress.Parse(         // Для его перевода в число используется
                        serverIp.Text);      // IPAddress.Parse
                int port =                   // Аналогично - порт
                    Convert.ToInt32(         // парсим число из текста
                        serverPort.Text);    // 
                endpoint =                   // endpoint - комбинация IP и порта
                    new(ip, port);           // 
                return endpoint;
            }
            catch
            {
                MessageBox.Show("Check server network parameters");
                return null;
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if ((endpoint = InitEndpoint()) is null) return;

            ChatMessage chatMessage = new()
            {
                Author = authorTextBox.Text,
                Text = messageTextBox.Text,
                Moment = DateTime.Now
            };
            SendMessage(chatMessage);
        }

        private void SendMessage(ChatMessage chatMessage)
        {
            if ((endpoint = InitEndpoint()) is null) return;

            Socket clientSocket = new(        // создаем сокет подключения
                AddressFamily.InterNetwork,   // адресация IPv4
                SocketType.Stream,            // Двусторонний сокет (и читать, и писать)
                ProtocolType.Tcp);            // Протокол сокета - ТСР
            try
            {
                clientSocket.Connect(endpoint);
                // --------------------- соединение установлено ----------------------
                // сервер начинает с приема данных, поэтому клиент начинает с посылки
                // формируем объект-запрос

                string dateTimeString;

                if (chatMessage.Moment.Date == DateTime.Today)
                {
                    dateTimeString = chatMessage.Moment.ToShortTimeString();
                }
                else
                {
                    dateTimeString = chatMessage.Moment.ToString();
                }
                if (string.IsNullOrEmpty(chatMessage.Text))
                {
                    chatMessage.Text = "Empty message";
                }
                ClientRequest request = new()
                {
                    Action = "Message",
                    Author = chatMessage.Author,
                    Text = chatMessage.Text,
                    Moment = chatMessage.Moment
                };

                SendRequest(clientSocket, request);
                var response = GetServerResponse(clientSocket);

                if (response is not null && response.Messages is not null)
                {
                    var message = response.Messages[0];
                    // chatLogs.Text += $"{message.Moment.ToShortTimeString()} {message.Author}: {message.Text}\n";
                    Label messageLabel = new()
                    {
                        Content = message.Text,
                        Background = Brushes.Salmon,
                        Margin = new Thickness(10, 5, 10, 5),
                        HorizontalAlignment = HorizontalAlignment.Right,
                    };
                    chatContainer.Children.Add(messageLabel);
                    // Задание: свои сообщения выравнивать по правому краю, чужие - по левому
                }
                else
                {
                    chatLogs.Text += "Ошибка доставки сообщения";
                }

                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Dispose();
            }
            catch (Exception ex)
            {
                chatLogs.Text += ex.Message + "\n";
            }
        }

        private async void ReCheckMessages()
        {
            // Проверить есть ли новые сообщения
            if ((endpoint = InitEndpoint()) is null) return;

            // новый запрос начинается с нового соединения
            Socket clientSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                clientSocket.Connect(endpoint);
                ClientRequest request = new()
                {
                    Action = "Get",
                    Author = authorTextBox.Text,
                    Moment = lastSyncMoment   // момент последней сверки сообщений
                };
                lastSyncMoment = DateTime.Now;   // обновляем момент последней сверки сообщений

                SendRequest(clientSocket, request);

                var response = GetServerResponse(clientSocket);

                if (response is not null && response.Messages is not null)
                {
                    foreach (var message in response.Messages)
                    {
                        // chatLogs.Text += $"{message.Moment.ToShortTimeString()} {message.Author}: {message.Text}\n";
                        Label messageLabel = new Label()
                        {
                            Content = message.Text,
                            Background = Brushes.Lime,
                            Margin = new Thickness(10, 5, 10, 5),
                            HorizontalAlignment = message.Author == "Me" ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                        };
                        chatContainer.Children.Add(messageLabel);
                    }
                }
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Dispose();

                // цикл - отложенный перезапуск
                await Task.Delay(1000);
                ReCheckMessages();
            }
            catch (Exception ex)
            {
                chatLogs.Text += ex.Message + "\n";
            }
        }

        private void SendRequest(Socket clientSocket, ClientRequest request)
        {
            // преобразуем объект в JSON
            String json = JsonSerializer.Serialize(request,
                new JsonSerializerOptions()
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            // отправляем на сервер
            clientSocket.Send(Encoding.UTF8.GetBytes(json));
        }

        private ServerResponse? GetServerResponse(Socket clientSocket)
        {
            MemoryStream stream = new();               // Другой способ получения
            do                                         // данных - собирать части
            {                                          // бинарного потока в 
                int n = clientSocket.Receive(buffer);  // память.
                stream.Write(buffer, 0, n);            // Затем создать строку
            } while (clientSocket.Available > 0);      // один раз пройдя
                                                       // все полученные байты.
            String str = Encoding.UTF8.GetString(stream.ToArray());
            // Декодируем его из JSON
            return JsonSerializer.Deserialize<ServerResponse>(str);
        }

    }
}

