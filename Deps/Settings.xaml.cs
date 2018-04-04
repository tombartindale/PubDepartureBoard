using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace DepartureBoard
{
    public sealed partial class Settings : UserControl
    {
        public Settings()
        {
            this.InitializeComponent();
            this.DataContext = AppSettings.Current;
            hostname.Text = $"Visit http://{GetLocalIp()}:8080/departureboard from your browser to update settings";
        }

        private string GetLocalIp()
        {
            var icp = NetworkInformation.GetInternetConnectionProfile();

            if (icp?.NetworkAdapter == null) return null;
            var hostname =
                NetworkInformation.GetHostNames()
                    .SingleOrDefault(
                        hn =>
                            hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                            == icp.NetworkAdapter.NetworkAdapterId);

            return hostname?.CanonicalName;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.Current.Save();
            this.Visibility = Visibility.Collapsed;
        }
    }
}
