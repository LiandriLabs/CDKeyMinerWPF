using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CDKeyMiner
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        App app;
        IMiner miner;
        int maxLogChars = 10000;

        public LogWindow()
        {
            InitializeComponent();
            app = (App)Application.Current;
            miner = app.Miner;
            app.MainWindow.Closing += MainWindow_Closing;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            miner.OnHashrate += Miner_OnHashrate;
            miner.OnTemperature += Miner_OnTemperature;
            miner.OnOutput += Miner_OnOutput;
        }

        private void Miner_OnHashrate(object sender, string e)
        {
            if (e.EndsWith("M") || e.EndsWith("MH/s"))
            {
                var num = double.Parse(Regex.Match(e, @"\d+").Value);
                hashRateChart.Dispatcher.Invoke(() =>
                {
                    hashRateChart.AddValue(num);
                });
            }
        }

        private void Miner_OnTemperature(object sender, int e)
        {
            tempChart.Dispatcher.Invoke(() =>
            {
                tempChart.AddValue(e);
            });
        }

        private void Miner_OnOutput(object sender, string e)
        {
            logsBox.Dispatcher.Invoke(() =>
            {
                if (logsBox.Text.Length > maxLogChars)
                {
                    var newLineStart = logsBox.Text.IndexOf('\n', maxLogChars / 2) + 1;
                    logsBox.Text = logsBox.Text.Substring(newLineStart, logsBox.Text.Length - newLineStart);
                }
                logsBox.Text += Environment.NewLine + e;
                logsBox.ScrollToEnd();
            });
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LogWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            miner.OnHashrate -= Miner_OnHashrate;
            miner.OnTemperature -= Miner_OnTemperature;
            miner.OnOutput -= Miner_OnOutput;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.Close();
        }

        private void ShowLogsButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", AppDomain.CurrentDomain.BaseDirectory + "Logs");
        }
    }
}
