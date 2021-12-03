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
        static string libPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib");
        static string phoenixExePath = Path.Combine(libPath, "PhoenixMiner.exe");
        static string nbminerExePath = Path.Combine(libPath, "nbminer.exe");
        string minerUsed;

        public Download()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("Page loaded: Download");
            minerUsed = Properties.Settings.Default.Plugin.ToLowerInvariant();

            if (minerUsed == "phoenix")
            {
                if (File.Exists(phoenixExePath))
                {
                    Log.Information("Phoenix Miner found, continue...");
                    app.DashboardPage = new Dashboard();
                    app.InfoPage = new Info();
                    NavigationService.Navigate(app.DashboardPage);
                }
                else
                {
                    DescLabel.Content = "Downloading Phoenix Miner...";
                    var sb = (Storyboard)FindResource("FadeIn");
                    sb.Begin(this);

                    Log.Information("Miner not found, downloading...");
                    using (var client = new WebClient())
                    {
                        client.DownloadProgressChanged += Client_DownloadProgressChanged;
                        client.DownloadFileAsync(
                            new Uri("https://app.cdkeyminer.com/static/downloads/Phoenix56d.exe"),
                            phoenixExePath + ".part"
                        );
                        client.DownloadFileCompleted += Client_DownloadFileCompleted;
                    }
                }
            }
            else if (minerUsed == "nbminer")
            {
                if (File.Exists(nbminerExePath))
                {
                    Log.Information("NBMiner found, continue...");
                    app.DashboardPage = new Dashboard();
                    app.InfoPage = new Info();
                    NavigationService.Navigate(app.DashboardPage);
                }
                else
                {
                    DescLabel.Content = "Downloading NBMiner...";
                    var sb = (Storyboard)FindResource("FadeIn");
                    sb.Begin(this);

                    Log.Information("Miner not found, downloading...");
                    using (var client = new WebClient())
                    {
                        client.DownloadProgressChanged += Client_DownloadProgressChanged;
                        client.DownloadFileAsync(
                            new Uri("https://app.cdkeyminer.com/static/downloads/nbminer401.exe"),
                            nbminerExePath + ".part"
                        );
                        client.DownloadFileCompleted += Client_DownloadFileCompleted;
                    }
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

            if (minerUsed == "phoenix")
            {
                File.Move(phoenixExePath + ".part", phoenixExePath);
            }
            else if (minerUsed == "nbminer")
            {
                File.Move(nbminerExePath + ".part", nbminerExePath);
            }
            
            app.DashboardPage = new Dashboard();
            app.InfoPage = new Info();
            NavigationService.Navigate(app.DashboardPage);
        }
    }
}
