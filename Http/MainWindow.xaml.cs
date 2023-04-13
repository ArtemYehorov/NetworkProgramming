using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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

namespace Http
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HttpClient httpClient;

        public MainWindow()
        {
            InitializeComponent();
            httpClient = new HttpClient();
        }

        private async void get1Button_Click(object sender, RoutedEventArgs e)
        {
            String result = await httpClient.GetStringAsync(url1TextBox.Text);
            resultTextBlock.Text = result;
        }

        private async void get2Button_Click(object sender, RoutedEventArgs e)
        {
            HttpRequestMessage request = new(   // Более полная форма запроса
                HttpMethod.Get,                 // Метод
                url2TextBox.Text);              // URL

            HttpResponseMessage response =            // Более полная форма
                await httpClient.SendAsync(request);  // ответа

            printResponse(response);
        }

        private async void head3Button_Click(object sender, RoutedEventArgs e)
        {
            var response = await httpClient.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Head,
                    url3TextBox.Text));

            printResponse(response);
        }

        private async void printResponse(HttpResponseMessage response)
        {
            resultTextBlock.Text = $"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}\n";

            foreach (var header in response.Headers)
            {   // var - KeyValuePair<string,IEnumerable<string>>
                String headerString = header.Key + ": ";
                foreach (string value in header.Value)
                {
                    headerString += value + " ";
                }
                resultTextBlock.Text += $"{headerString}\n";
            }
            resultTextBlock.Text += "------------------------------------\n";
            resultTextBlock.Text += await response.Content.ReadAsStringAsync();
            resultTextBlock.Text += "\n------------------------------------\n";
        }

        private async void printResponsePassword(HttpResponseMessage response)
        {
            resultTextBlock.Text = $"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}\n";

            string responseBody = await response.Content.ReadAsStringAsync();
            string pattern = @"<p>Here are your random passwords:</p>(.*?)</ul>";
            Match match = Regex.Match(responseBody, pattern, RegexOptions.Singleline);
            if (match.Success)
            {
                string passwordBlock = match.Groups[1].Value;
                string cleanPasswords = Regex.Replace(passwordBlock, "<.*?>", string.Empty);
                resultTextBlock.Text += $"{cleanPasswords}\n";
            }
            else
            {
                resultTextBlock.Text += responseBody;
            }
        }

        private async void options4Button_Click(object sender, RoutedEventArgs e)
        {
            var response = await httpClient.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Options,
                    url4TextBox.Text));

            printResponse(response);
        }

        private async void get5Button_Click(object sender, RoutedEventArgs e)
        {
            var response = await httpClient.SendAsync(
                new HttpRequestMessage(
                    HttpMethod.Get,
                    url5TextBox.Text));

            printResponsePassword(response);
        }
    }
}
/* Современный подход к взаимодействию по НТТР - HttpClient
 * Объект этого класса есть смысл делать единственным и использовать
 * для разных запросов.
 * Выполнения запросов асинхронны, значит нужно применять соответствующие технологии
 * (async / await / ContinueWith)
 * httpClient.GetStringAsync - возвращает только тело
 * httpClient.SendAsync - возвращает все данные, но в два приема
 * 1) статус-код и заголовки
 * 2) тело (контент)
 * !! Для полного выполнения одного запроса идет два await
 * response = await httpClient.SendAsync(request)
 * ...
 * body = await response.Content.ReadAsStringAsync();
 * 
 * Запросы методом HEAD возвращают только заголовки, тело - пустое
 * такие запросы выполняются быстро и используються для проверки доступности
 * сайта (ресурса)
 * 
 * Запрос OPTIONS разведывает какие дополнительные опции поддерживает 
 * данный сайт. Обычно в ответе только заголовки с перечнем опций,
 * например, допустимые методы запросов, допустимые заголовки (кроме стандартных)
 * Браузеры автоматически посылают такие запросы перед тем, как
 * отправить на сайт запросы любым методом, кроме GET, POST
 * !! запрос из браузера методом DELETE - это два запроса
 * 1) OPTIONS
 * 2) по рез-ту ответа - DELETE (если ответ разрешил его использовать)
 */
/* Выполнить GET запрос на адрес https://www.random.org/passwords/?num=5&len=8&format=html&rnd=new
 * При помощи поиска / регулярных выражений найти блок
 * <p>Here are your random passwords:</p>
 * И выделить данные из элементов 
 * <li>FCu5k44P</li>
 * Как результат показать 5 предложенных вариантов случайных паролей
 */

