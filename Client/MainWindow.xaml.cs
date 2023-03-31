using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            IPEndPoint endPoint;
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
            Socket ClientSocket = new(           
               AddressFamily.InterNetwork,
               SocketType.Stream,         
               ProtocolType.Tcp);
            try
            {
                ClientSocket.Connect(endPoint);
                ClientSocket.Send(Encoding.UTF8.GetBytes(messageTextBox.Text));

                ClientSocket.Shutdown(SocketShutdown.Both);
                ClientSocket.Dispose();
            }
            catch (Exception ex)
            {
                ChatLogs.Text += ex.Message + "\n";
            }
           
        }
    }
}
