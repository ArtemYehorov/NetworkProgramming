using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text.Json;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Socket? ListenSocket; // Слушающий сокет - постоянно активный пока сервер включен
        private List<ChatMessage> messages; //все приходящие сообщения сохранятся 
        public MainWindow()
        {
            InitializeComponent();
            messages = new();
        }
        private void StartServer_Click(object sender, RoutedEventArgs e)
        {
            IPEndPoint endPoint;
            try
            {
                IPAddress ip = IPAddress.Parse(ServerIp.Text); //На окне Ip - это текст ("127.0.0.1"). для его перевода в число, используется  IPAddress.Parse(текст)
                int Port = Convert.ToInt32(ServerPort.Text); //Аналогично - порт парсим число из текста
                endPoint = new(ip, Port); //endpoint - комбинация ip + port
            }
            catch 
            {
                MessageBox.Show("Check start Network parameters");
                return;
            }
            ListenSocket = new(            // создаём слущающий сокет 
                AddressFamily.InterNetwork,// адресация IPv4
                SocketType.Stream,         // Двухсторонний сокет(и читать, и писать)
                ProtocolType.Tcp);         // Протокол сокета - TCP
           
            // Постоянно активный сокет(слущающий сокет) заблокирует UI если его не отделить в свой поток
            new Thread(StartServerMethod).Start(endPoint);
        }

        private void StartServerMethod(object? param)
        {
            if (ListenSocket is null) return;
            IPEndPoint endPoint = param as IPEndPoint;
            if (endPoint is null) return;

            try
            {
                ListenSocket.Bind(endPoint);                                //привязываем сервер к endPoint
                ListenSocket.Listen(100);                                   // стартуем прослушку, разрешаем очередь из 100 сообщений
                Dispatcher.Invoke(() =>                                 
                   ServerLogs.Text += "Server Started\n");
                byte[] buf = new byte[1024];                               //буфер приёма данных 

                Dispatcher.Invoke(() =>
                 ServerStatus.Content = "ON");
                Dispatcher.Invoke(() =>
                ServerStatus.Background = Brushes.Green);
            
                while (true)                                               //бессконечный цикл прослушки порта, внутри которого мы
                {
                    Socket socket = ListenSocket.Accept();                 // Ожидание подключения и создание уже обменного сокета с клиентом 
                    //начинаем приём данных 
                    StringBuilder sb = new StringBuilder();
                    do
                    {
                        int n = socket.Receive(buf);                        // Сохраняем порцию данных в буфер и получаем количество переданных байтов (n)
                        sb.Append(Encoding.UTF8.GetString(buf, 0, n));      // Переводим полученные байты в строку, согластно кодировке UTF8 и накапливаем в StringBuilder
                    } while (socket.Available > 0);                         // Пока есть данные в сокете 
                                                                            
                    String str = sb.ToString();                             // Собираем все фрагменты в одну строку 
                   // Dispatcher.Invoke(() =>                                 // Добавляем полученные данные к логам сервера. Используем  Dispatcher.Invoke для доступа к UI
                   // ServerLogs.Text += str + "\n");

                    //Разбираем JSON
                    var request = JsonSerializer.Deserialize<ClientRequest>(str);
                    //Определяем ти запроса (action) и готовим ответ
                    ServerResponse response = new();
                    switch (request?.Action)
                    {
                        case "Message":
                            //извлекаем сообщение из запроса 
                            ChatMessage message = new()
                            {
                                Author = request.Author,
                                Text = request.Text,
                                Moment = request.Moment
                            };
                            //Сохраняем его в коллекции сервера
                            messages.Add(message);
                            //Передаём его же как подтвержение получения
                            response.Status = "OK";
                            response.Messages = new() { message};
                            Dispatcher.Invoke(() => ServerLogs.Text += $"{request.Moment.ToShortTimeString()} {request.Author}: {request.Text}");
                            break;
                        case "Get":
                            String Author = request.Author;
                            DateTime LastSyncMoment = request.Moment;
                            //Собираем сообщение НЕ данного автора, время которых больше чем LastSyncMoment
                            response.Status = "OK";
                            response.Messages = new();
                            foreach (var m in messages)
                            {
                                if (!m.Author.Equals(Author) && m.Moment > LastSyncMoment)
                                {
                                    response.Messages.Add(m);
                                }
                            }

                            break;
                        default:
                            response.Status = "Error!";
                            break;
                    }


                    // Отправляем клиенту ответ 
                    str = JsonSerializer.Serialize(request, new JsonSerializerOptions() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });   
                    // В обратном порядке -сначала строка                
                    socket.Send(Encoding.UTF8.GetBytes(str));               // затем переводим в байты по заданной кодировке и отправляем в socket
                                                                            
                                                                            
                    socket.Shutdown(SocketShutdown.Both);                   // Закрываем соеденение - отключаем сокет с уведомление клиента.
                    socket.Dispose();                                       // Освобождаем ресурс
                }                                                           
            }                                                               
            catch(Exception ex)                                             
            {                                                               
                Dispatcher.Invoke(() =>                                     // логируем исключение
                  ServerLogs.Text += "Server stopped" + ex.Message + "\n"); // уведомляем об остановке сервера

                Dispatcher.Invoke(() => ServerStatus.Content = "OFF");
                Dispatcher.Invoke(() => ServerStatus.Background = Brushes.Red);
            }
        }

        private void StopServer_Click(object sender, RoutedEventArgs e)
        {
            // Остановить бессконечный цикл можно только выбросом исключения 
            Dispatcher.Invoke(() => ServerStatus.Content = "OFF");
            Dispatcher.Invoke(() => ServerStatus.Background = Brushes.Red);
            ListenSocket?.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ListenSocket != null)
            {
                StopServer_Click(null, null);
            }
        }
    }
}
