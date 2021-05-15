﻿using System;
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
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var uname = Properties.Settings.Default.Username;
            if (!string.IsNullOrEmpty(uname))
            {
                NavigationService.Navigate(new Dashboard(new Credentials(uname, "x")));
            }
            else
            {
                usernameBox.Focus();
                var sb = (Storyboard)FindResource("FadeIn");
                sb.Begin(this);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fadeOut = (FindResource("FadeOut") as Storyboard).Clone();
                fadeOut.Begin(this);
                messageLabel.Visibility = Visibility.Collapsed;

                Log.Information("Trying to log in");
                var loggedIn = await LoginHelper.LogIn(usernameBox.Text, passwordBox.Password);
                if (loggedIn)
                {
                    Log.Information("Logged in");
                    Properties.Settings.Default.Username = usernameBox.Text;
                    Properties.Settings.Default.Save();
                    NavigationService.Navigate(new Dashboard(new Credentials(usernameBox.Text, passwordBox.Password)));
                }
                else
                {
                    Log.Error("Login failed");
                    messageLabel.Content = "Username or password incorrect";
                    messageLabel.Visibility = Visibility.Visible;
                    usernameBox.Focus();
                    var fadeIn = (FindResource("FadeIn") as Storyboard).Clone();
                    fadeIn.Begin(this);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Communication error");
                messageLabel.Content = "Cannot connect to server";
                messageLabel.Visibility = Visibility.Visible;
                var sb = (FindResource("FadeIn") as Storyboard).Clone();
                sb.Begin(this);
            }
        }

        private void usernameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (usernameBox.Text == "")
            {
                ImageBrush textImageBrush = new ImageBrush();
                textImageBrush.ImageSource =
                    new BitmapImage(
                        new Uri(@"pack://application:,,,/CDKeyMiner;component/Resources/username.gif", UriKind.Absolute)
                    );
                textImageBrush.AlignmentX = AlignmentX.Left;
                textImageBrush.Stretch = Stretch.None;
                usernameBox.Background = textImageBrush;
            }
            else
            {
                usernameBox.Background = Brushes.White;
            }
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (passwordBox.Password == "")
            {
                ImageBrush textImageBrush = new ImageBrush();
                textImageBrush.ImageSource =
                    new BitmapImage(
                        new Uri(@"pack://application:,,,/CDKeyMiner;component/Resources/password.gif", UriKind.Absolute)
                    );
                textImageBrush.AlignmentX = AlignmentX.Left;
                textImageBrush.Stretch = Stretch.None;
                passwordBox.Background = textImageBrush;
            }
            else
            {
                passwordBox.Background = Brushes.White;
            }
        }

        private void usernameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                passwordBox.Focus();
            }
        }

        private void passwordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Button_Click(null, null);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });
            e.Handled = true;
        }
    }
}
