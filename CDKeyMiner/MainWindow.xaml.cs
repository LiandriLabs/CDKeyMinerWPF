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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Serilog;

namespace CDKeyMiner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ContextMenu appMenu;

        public MainWindow()
        {
            InitializeComponent();
            _mainFrame.Navigate(new LoginPage());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //var sb = (Storyboard)FindResource("FadeIn");
            //sb.Begin(mainWindow);
            appMenu = this.FindResource("AppMenu") as ContextMenu;
            if (appMenu != null)
            {
                var theme = (Application.Current as App).Theme;
                if (theme == "Light")
                {
                    (appMenu.Items[1] as MenuItem).IsChecked = true;
                }
                else if (theme == "Dark")
                {
                    (appMenu.Items[0] as MenuItem).IsChecked = true;
                }
                else if (theme == "Glass")
                {
                    (appMenu.Items[2] as MenuItem).IsChecked = true;
                }

                if (Properties.Settings.Default.EcoMode)
                {
                    (appMenu.Items[4] as MenuItem).IsChecked = true;
                }
            }
            
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            App.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            SettingsButton.Visibility = Visibility.Collapsed;
            Properties.Settings.Default.Username = "";
            Properties.Settings.Default.JWT = "";
            Properties.Settings.Default.Save();
            (_mainFrame.Content as Dashboard)?.StopMiner();
            balanceLbl.AnimatedUpdate("");
            _mainFrame.Navigate(new LoginPage());
        }

        private void MenuDark_Click(object sender, RoutedEventArgs e)
        {
            if ((Application.Current as App).Theme != "Dark")
            {
                Properties.Settings.Default.Theme = "Dark";
                Properties.Settings.Default.Save();
                RestartApplication();
            }
        }

        private void MenuLight_Click(object sender, RoutedEventArgs e)
        {
            if ((Application.Current as App).Theme != "Light")
            {
                Properties.Settings.Default.Theme = "Light";
                Properties.Settings.Default.Save();
                RestartApplication();
            }
        }

        private void MenuGlass_Click(object sender, RoutedEventArgs e)
        {
            if ((Application.Current as App).Theme != "Glass")
            {
                Properties.Settings.Default.Theme = "Glass";
                Properties.Settings.Default.Save();
                RestartApplication();
            }
        }

        private void RestartApplication()
        {
            var pid = Process.GetCurrentProcess().Id;
            Process.Start("CDKeyMiner.exe", pid.ToString());
            Application.Current.MainWindow.Close();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = this.FindResource("AppMenu") as ContextMenu;
            cm.PlacementTarget = sender as Button;
            cm.IsOpen = true;
        }

        private void EcoMode_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.EcoMode = !Properties.Settings.Default.EcoMode;
            Properties.Settings.Default.Save();
            Phoenix.Instance.Restart();
        }
    }
}
