using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Deps
{

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
            Loaded += MainPage_Loaded;
        }

        private async void Engine_OnLatestData(DateTime obj)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                updatedat.DataContext = null;
                updatedat.DataContext = obj;
            });
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await engine.InitLoad();
            //datasource = new CollectionViewSource();
            alltrains.ItemsSource = engine.CurrentTrains;
            //datasource.Source = engine.CurrentBoard.trainServices;
            //alltrains.ItemsSource = datasource;
           
        }
        

        private async void Engine_OnUpdate()
        {
            //Debug.WriteLine(engine.CurrentBoard.trainServices.Count());
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                //should have to do nothing...
                alltrains.ItemsSource = null;
                alltrains.ItemsSource = engine.CurrentTrains;
            });
            
            //await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal),
            //alltrains.ItemsSource = engine.CurrentBoard;
        }
    }
}
