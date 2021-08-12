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
using System.IO;
using System.Net;
using Serilog;
using System.Windows.Media.Animation;

namespace CDKeyMiner
{
    /// <summary>
    /// Interaction logic for Download.xaml
    /// </summary>
    public partial class Download : Page
    {
        App app = (App)Application.Current;

        public Download()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("Page loaded: Download");
            string libPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib");
            var minerExePath = Path.Combine(libPath, "PhoenixMiner.exe");
            if (File.Exists(minerExePath))
            {
                Log.Information("Miner found, continue...");
                app.DashboardPage = new Dashboard();
                app.InfoPage = new Info();
                NavigationService.Navigate(app.DashboardPage);
            }
            else
            {
                var sb = (Storyboard)FindResource("FadeIn");
                sb.Begin(this);

                Log.Information("Miner not found, downloading...");
                using (var client = new WebClient())
                {
                    client.DownloadProgressChanged += Client_DownloadProgressChanged;
                    client.DownloadFileAsync(
                        new Uri("https://app.cdkeyminer.com/static/downloads/Phoenix.exe"),
                        minerExePath
                    );
                    client.DownloadFileCompleted += Client_DownloadFileCompleted;
                }
            }
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadProgress.Value = e.ProgressPercentage;
        }

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Log.Information("Download finished");
            app.DashboardPage = new Dashboard();
            app.InfoPage = new Info();
            NavigationService.Navigate(app.DashboardPage);
        }
    }
}
