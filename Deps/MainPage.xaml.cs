using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Deps
{

    public class TimeLeftConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return $"TTL {((TimeSpan)value).ToString("%m")}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }

    }

    public class StatusIconConverter:IValueConverter
    {
        static string getnum(int number)
        {
            int sub_num = 0x2080;
            string output = "";
            foreach (var n in number.ToString())
            {
                int c = int.Parse(n.ToString()) + sub_num;
                output += char.ConvertFromUtf32(c);
            }

            return output;
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var train = value as trainServicesService;
            //return "\uf098" + getnum((int)train.TimeLeft.TotalMinutes - (int)Engine.BoardStatus.RUNNOW) + "\uf583\u2086";

            switch (train.CurrentStatus)
            {
                case Engine.BoardStatus.DRINKUP:
                    return "\uf152\uf098" + getnum((int)train.TimeLeft.TotalMinutes) + " +\uf583" + getnum((int)Engine.BoardStatus.GONOW);

                case Engine.BoardStatus.GONOW:
                    return "\uf583" + getnum((int)(train.Expected - DateTime.Now.TimeOfDay).TotalMinutes);

                case Engine.BoardStatus.GETDRINK:
                    return "\uf098" + getnum((int)train.TimeLeft.TotalMinutes) + " +\uf583" +  getnum((int)Engine.BoardStatus.GONOW);

                case Engine.BoardStatus.NORMAL:
                    return "\uf098" + getnum((int)train.TimeLeft.TotalMinutes + (int)Engine.BoardStatus.GONOW);

                case Engine.BoardStatus.RUNNOW:
                    return "\uf46e" + getnum((int)(train.Expected - DateTime.Now.TimeOfDay).TotalMinutes);

                case Engine.BoardStatus.TOOLATE:
                    return "\uf098?";
                default:
                    return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Engine engine = new Engine();

        public MainPage()
        {
            InitializeComponent();
            engine.OnUpdate += Engine_OnUpdate;
            engine.OnLatestData += Engine_OnLatestData;
            engine.OnWeatherUpdate += Engine_OnWeatherUpdate;
            Loaded += MainPage_Loaded;
        }

        private async void Engine_OnWeatherUpdate(string obj)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                //weather.Text = $"It be {obj} outside";
                //mainweather.Source = new BitmapImage(new Uri("ms-appx://Deps/Assets/" + obj + ".png"));
                switch(obj)
                {
                    case "clear-day":
                        mainweather.Text = "\uf00d";
                        break;
                    case "clear-night":
                        mainweather.Text = "\uf02e";
                        break;
                    case "rain":
                        mainweather.Text = "\uf019";
                        break;
                    case "snow":
                        mainweather.Text = "\uf01b";
                        break;
                    case "sleet":
                        mainweather.Text = "\uf0b5";
                        break;
                    case "wind":
                        mainweather.Text = "\uf050";
                        break;
                    case "fog":
                        mainweather.Text = "\uf014";
                        break;
                    case "cloudy":
                        mainweather.Text = "\uf013";
                        break;
                    case "partly-cloudy-day":
                        mainweather.Text = "\uf002";
                        break;
                    case "partly-cloudy-night":
                        mainweather.Text = "\uf086";
                        break;
                }
            });
        }

        private async void Engine_OnLatestData(DateTime obj)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                updatedat.DataContext = null;
                updatedat.DataContext = $"updated at {obj.ToString("HH:mm")}";
            });
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.UnhandledException += Current_UnhandledException;

            await engine.InitLoad();
            alltrains.ItemsSource = engine.CurrentTrains;
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(1);
            timer.Tick += Timer_Tick;
            timer.Start();
            Timer_Tick(null, null);
        }

        private void Current_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Environment.FailFast(e.Message);
        }

        private void Timer_Tick(object sender, object e)
        {
            time.Text = DateTime.Now.ToString("HH:mm");
        }

        private async void Engine_OnUpdate()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                //should have to do nothing...
                alltrains.ItemsSource = null;
                alltrains.ItemsSource = engine.CurrentTrains;
            });
        }
    }
}
