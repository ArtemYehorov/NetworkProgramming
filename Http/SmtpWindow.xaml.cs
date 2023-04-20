using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
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
using System.Windows.Shapes;

namespace Http
{
    /// <summary>
    /// Логика взаимодействия для SmtpWindow.xaml
    /// </summary>
    public partial class SmtpWindow : Window
    {
        private dynamic? emailConfig;
        public SmtpWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            /* Открываем файл конфигурации и пытаемся извлечь данные 
             */
            String configFilename = "emailconfig.json";
            try
            {
                //Для универсальности создаём динамический тип для данных
                emailConfig = JsonSerializer.Deserialize<dynamic>(
                        System.IO.File.ReadAllText(configFilename)
                );
            }
            catch (System.IO.FileNotFoundException)
            {
                MessageBox.Show($"Не найден файл конфигурации '{configFilename}'");
                this.Close();
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Ошбика преобразования конфигурации '{ex.Message}'");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошбика обработки конфигурации '{ex.Message}'");
                this.Close();
            }
            if (emailConfig is null)
            {
                MessageBox.Show($"Ошбика получения конфигурации ");
                this.Close();
            }
        }

        private SmtpClient GetSmtpClient()
        {
            if (emailConfig is null)
            {
                return null;
            }

            /*Динамические объекты позволяют разименовывать свои поля как */
            // emailConfig.GetProperty("smpt").GetProperty("gmail").GetProperty("host").GetString();
            JsonElement gmail = emailConfig.GetProperty("smpt").GetProperty("gmail");

            String host = gmail.GetProperty("host").GetString()!;
            int port = gmail.GetProperty("port").GetInt32();
            String mailbox = gmail.GetProperty("email").ToString()!;
            String password = gmail.GetProperty("password").GetString()!;
            bool ssl = gmail.GetProperty("ssl").GetBoolean();

            return new(host)
            {
                Port = port,
                EnableSsl = ssl,
                Credentials = new NetworkCredential(mailbox, password)
            };
        }

        private void SendTextButton_Click(object sender, RoutedEventArgs e)
        {
            using SmtpClient smtpClient = GetSmtpClient();
            JsonElement gmail = emailConfig.GetProperty("smpt").GetProperty("gmail");
            String mailbox = gmail.GetProperty("email").ToString()!;

            try
            {
                smtpClient.Send(
                    from: mailbox, 
                    recipients: "artemyehorov@gmail.com",
                    subject: "Test Message", 
                    body: "Test message from SmtpWindow");

                MessageBox.Show("Sent OK!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sent error '{ex.Message}'");
            }
        }

        private void SendHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            using SmtpClient smtpClient = GetSmtpClient();
            JsonElement gmail = emailConfig.GetProperty("smpt").GetProperty("gmail");
            String mailbox = gmail.GetProperty("email").ToString()!;
            MailMessage mailMessage = new MailMessage()
            {
                From = new MailAddress(mailbox),
                Body = "<b>Test</b> <i>message<i/> from <b style='color:green'>SmtpWindow</b>",
                IsBodyHtml = true,
                Subject = "Test Message"

            };
            mailMessage.To.Add(new MailAddress("artemyehorov@gmail.com"));

            try
            {
                smtpClient.Send(mailMessage);

                MessageBox.Show("Sent OK!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sent error '{ex.Message}'");
            }
        }

        private void SendRandomCodeButton_Click(object sender, RoutedEventArgs e)
        {
            using SmtpClient smtpClient = GetSmtpClient();
            JsonElement gmail = emailConfig.GetProperty("smpt").GetProperty("gmail");
            String mailbox = gmail.GetProperty("email").ToString()!;
            MailMessage mailMessage = new MailMessage()
            {
                From = new MailAddress(mailbox),
                Body = $"<b>Ваш случайный пароль из 8 символов: </b> <b style='color:red'>{RandomCode()}</b>",
                IsBodyHtml = true,
                Subject = "Test Message"

            };
            mailMessage.To.Add(new MailAddress("artemyehorov@gmail.com"));

            try
            {
                smtpClient.Send(mailMessage);

                MessageBox.Show("Sent OK!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sent error '{ex.Message}'");
            }
        }

        private String RandomCode()
        {
            string chars = "abcdefghiklmnpqrstuvwxy0123456789";
            Random random = new Random();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 8; i++)
            {
                builder.Append(chars[random.Next(chars.Length)]);
            }
            string randomString = builder.ToString();
            return randomString;
        }
    }
}
