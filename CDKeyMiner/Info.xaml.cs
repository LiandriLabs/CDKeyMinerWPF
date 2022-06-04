using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace CDKeyMiner
{
    /// <summary>
    /// Interaction logic for Info.xaml
    /// </summary>
    public partial class Info : Page
    {
        App app = (App)Application.Current;
        Window logWindow;

        public Info()
        {
            InitializeComponent();
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(app.DashboardPage);
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateButton.Content = "UPDATING...";
            UpdateButton.IsEnabled = false;
            await Updater.Instance.DownloadUpdates();
            MessageBox.Show("Update finished, click OK to restart");
            var pid = Process.GetCurrentProcess().Id;
            Process.Start("CDKeyMiner.exe", pid.ToString());
            Application.Current.MainWindow.Close();
        }

        private void ShowLogsButton_Click(object sender, RoutedEventArgs e)
        {
            if (logWindow == null)
            {
                logWindow = new LogWindow();
                logWindow.Closed += LogWindow_Closed;
            }
            logWindow.Show();
        }

        private void LogWindow_Closed(object sender, EventArgs e)
        {
            logWindow.Closed -= LogWindow_Closed;
            logWindow = null;
        }
    }
}
