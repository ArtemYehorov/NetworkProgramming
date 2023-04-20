using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
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
    /// Interaction logic for CoincapWindow.xaml
    /// </summary>
    public partial class CoincapWindow : Window
    {
        public ObservableCollection<Asset> Assets { get; set; }

        private HttpClient _httpClient = new();
        private int indexShown;  // индекс ассета, изображенного на графике

        public CoincapWindow()
        {
            InitializeComponent();
            Assets = new();
            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAssets();
        }

        private async void LoadAssets()
        {
            var assetsResponse = await _httpClient.GetFromJsonAsync<AssetsResponse>(
                "https://api.coincap.io/v2/assets");
            if (assetsResponse is null)
            {
                MessageBox.Show("JSON error");
                return;
            }
            indexShown = 4;
            ShowHistory(assetsResponse.data[indexShown]);

            Assets.Clear();
            foreach (Asset asset in assetsResponse.data)
            {
                Assets.Add(asset);
            }
            AssetsListView.SelectedIndex = indexShown;
        }

        private async void ShowHistory(Asset asset)
        {
            String url = $"https://api.coincap.io/v2/assets/{asset.id}/history?interval=d1";
            var historyResponse = await _httpClient.GetFromJsonAsync<HistoryResponse>(url);
            if (historyResponse is null)
            {
                MessageBox.Show("JSON history error");
                return;
            }
            // Построение графика
            // 1. Подготовка - определение граничных значений
            long minTime, maxTime;
            double minRate, maxRate;
            minTime = historyResponse.data.Min(r => r.time);
            maxTime = historyResponse.data.Max(r => r.time);
            minRate = historyResponse.data.Min(r => r.priceUsd);
            maxRate = historyResponse.data.Max(r => r.priceUsd);
            double graphHeight = GraphCanvas.ActualHeight;
            double graphWidth = GraphCanvas.ActualWidth;

            // 2. Проходим массив данных, пересчитываем точку на графике, формируем линии
            double x1, y1, x2, y2;
            double kx = graphWidth / (maxTime - minTime);
            const double offset = 0.05;  // 5% отступ от границ графика (сверху и снизу)
            double h = graphHeight * (1 - offset);
            double ky = graphHeight / (maxRate - minRate) * (1 - 2 * offset);

            x1 = // шкала времени от minTime до maxTime ==> 0 .. graphWidth
                (historyResponse.data[0].time - minTime) * kx;
            y1 = // шкала Y перевернута и используем отступы offset от "краев"
                h - (historyResponse.data[0].priceUsd - minRate) * ky;

            foreach (Rate rate in historyResponse.data)
            {
                x2 = (rate.time - minTime) * kx;
                y2 = h - (rate.priceUsd - minRate) * ky;
                DrawLine(x1, y1, x2, y2);
                x1 = x2;
                y1 = y2;
            }
        }

        private void DrawLine(double fromX, double fromY, double toX, double toY)
        {
            Line line = new()
            {
                X1 = fromX,
                Y1 = fromY,
                X2 = toX,
                Y2 = toY,
                Stroke = new SolidColorBrush(Colors.BlueViolet),
                StrokeThickness = 1
            };
            GraphCanvas.Children.Add(line);
        }

        private void AssetsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (indexShown == AssetsListView.SelectedIndex)
            {
                // на графике и так этот элемент - перерисовывать не надо
                return;
            }
            MessageBox.Show(AssetsListView.SelectedIndex.ToString());
            /* Д.З. Реализовать отображение нового графика при выборе
             * нового ассета в приведенном списке (не забывать очищать 
             * GraphCanvas.Children)
             * Вывести в заголовке "Rate history" название ассета
             * "Rate history: Bitcoin"
             * Возле холста (графика) на уровнях 0.05 и 0.95 добавить
             * Label-ы в которые выводить минимальное и максимальное 
             * значения курсов данного ассета
             * ** Использовать случайные / индивидуальные цвета для построения
             *    новых графиков
             */
        }
    }

    // ORM - используем инструмент https://json2csharp.com/
    public class Asset
    {
        public string id { get; set; }
        public string rank { get; set; }
        public string symbol { get; set; }
        public string name { get; set; }
        public string supply { get; set; }
        public string maxSupply { get; set; }
        public string marketCapUsd { get; set; }
        public string volumeUsd24Hr { get; set; }
        public double priceUsd { get; set; }
        public string changePercent24Hr { get; set; }
        public string vwap24Hr { get; set; }
        public string explorer { get; set; }
    }
    public class AssetsResponse
    {
        public List<Asset> data { get; set; }
        public long timestamp { get; set; }
    }

    public class Rate
    {
        public double priceUsd { get; set; }
        public long time { get; set; }
        public DateTime date { get; set; }
    }
    public class HistoryResponse
    {
        public List<Rate> data { get; set; }
        public long timestamp { get; set; }
    }

}