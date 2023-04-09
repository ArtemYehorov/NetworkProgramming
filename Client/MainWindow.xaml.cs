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
        IPEndPoint? endPoint;
        DateTime LastSuncMoment;

        public MainWindow()
        {
            InitializeComponent();
            LastSuncMoment = DateTime.MinValue;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что поля для ввода текста сообщения и имени автора не пусты
            if (string.IsNullOrWhiteSpace(messageTextBox.Text) || string.IsNullOrWhiteSpace(AuthorTextBox.Text))
            {
                MessageBox.Show("Please enter message and author");
                return;
            }
            if (endPoint is null) //Первое нажатие - определяем сервер
            {
                try
                {
                    IPAddress ip = IPAddress.Parse(ServerIp.Text);
                    int Port = Convert.ToInt32(ServerPort.Text);
                    endPoint = new(ip, Port);
                }
                catch
                {
                    MessageBox.Show("Check start Network parameters");
                    return;
                }
            }

            ChatMessage chatMessage = new ChatMessage()
            {
                Text = messageTextBox.Text,
                Author = AuthorTextBox.Text,
                Moment = DateTime.Now
            };
            SendMessage(chatMessage);
        }

        private void SendMessage(ChatMessage message)
        {
            // проверяем, что сообщение и автор не пустые
            if (string.IsNullOrEmpty(message.Author) || string.IsNullOrEmpty(message.Text))
            {
                MessageBox.Show("Author and message cannot be empty.");
                return;
            }

            if (endPoint is null) return;
           
            Socket ClientSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                ClientSocket.Connect(endPoint);
                //формируем объект запроса
                ClientRequest request = new()
                {
                    Action = "Message",
                    Author = message.Author,
                    Text = message.Text,
                    Moment = message.Moment
                };
                // преобразуем объект в JSON
                String json = JsonSerializer.Serialize(request,           // Для Юникода в JSON
                    new JsonSerializerOptions()
                    {                         // используются \uXXXX
                        Encoder = System.Text.Encodings.Web               // выражения. Чтобы 
                        .JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });  // был обычный текст - Encoder
                // отправляем на сервер
                ClientSocket.Send(Encoding.UTF8.GetBytes(json));

                // после приема сервер отправляет подтверждение, клиент - получает
                MemoryStream stream = new();               // Другой способ получения
                do                                         // данных - собирать части
                {                                          // бинарного потока в 
                    int n = ClientSocket.Receive(buffer);  // память.
                    stream.Write(buffer, 0, n);            // Затем создать строку
                } while (ClientSocket.Available > 0);      // один раз пройдя
                String str = Encoding.UTF8.GetString(      // все полученные байты.
                    stream.ToArray());

                ClientSocket.Shutdown(SocketShutdown.Both);
                ClientSocket.Dispose();
            }
            catch (Exception ex)
            {
                ChatLogs.Text += ex.Message + "\n";
            }
        }

        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            if (endPoint is null) return;
            // проверить есть ли новые сообщения

            //Новый запрос начинается с нового соеденения
            Socket ClientSocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                ClientSocket.Connect(endPoint);
                ClientRequest request = new()
                {
                    Action = "Get",
                    Author = AuthorTextBox.Text,
                    Moment = LastSuncMoment //момент последней сверки сообщения
                };
                LastSuncMoment = DateTime.Now;

            // преобразуем объект в JSON
            String json = JsonSerializer.Serialize(request,       
                    new JsonSerializerOptions()
                    {                         
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    }); 
                ClientSocket.Send(Encoding.UTF8.GetBytes(json));

                //получаем ответ
                MemoryStream stream = new();             
                do                                       
                {                                        
                    int n = ClientSocket.Receive(buffer);
                    stream.Write(buffer, 0, n);          
                } while (ClientSocket.Available > 0);    
                String str = Encoding.UTF8.GetString(stream.ToArray());
                //декодируем его из JSON 
                var response = JsonSerializer.Deserialize<ServerResponse>(str);
                if (response is not null && response.Messages is not null) 
                {
                    foreach (var message in response.Messages) 
                    {
                        ChatLogs.Text += $"{message.Moment.ToShortTimeString()} {message.Author}: {message.Text}\n";
                    }
                }
                ClientSocket.Shutdown(SocketShutdown.Both);
                ClientSocket.Dispose();
            }
            catch (Exception ex) 
            {
                ChatLogs.Text += ex.Message;
            }
        }
    }
}
