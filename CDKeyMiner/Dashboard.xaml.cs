﻿using System;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CDKeyMiner
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Page
    {
        private Credentials creds;
        private bool mining = false;
        private IMiner miner;
        private int shares = 0;

        public Dashboard(Credentials credentials)
        {
            InitializeComponent();
            creds = credentials;
        }

        ~Dashboard()
        {
            if (miner != null)
            {
                miner.Stop();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var sb = (Storyboard)FindResource("FadeIn");
            sb.Begin(this);
        }

        private void Label_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Opacity = 0;

            if (!mining)
            {
                mining = true;
                buttonLbl.Content = "■";
                statusLbl.Content = "Starting miner...";
                miner = new Trex();
                miner.OnError += (s, err) =>
                {
                    if (err == MinerError.ExeNotFound)
                    {
                        statusLbl.Dispatcher.Invoke(() =>
                        {
                            buttonLbl.Content = "!";
                            statusLbl.Content = "Cannot find miner EXE.";
                        });
                    }
                    else if (err == MinerError.ConnectionError)
                    {
                        statusLbl.Dispatcher.Invoke(() =>
                        {
                            buttonLbl.Content = "!";
                            statusLbl.Content = "Connection error (retrying).";
                        });
                    }
                };
                miner.OnAuthorized += (s, evt) =>
                {
                    statusLbl.Dispatcher.Invoke(() =>
                    {
                        buttonLbl.Content = "■";
                        statusLbl.Content = "Miner connected.";
                    });
                };
                miner.OnMining += (s, evt) =>
                {
                    statusLbl.Dispatcher.Invoke(() =>
                    {
                        buttonLbl.Content = "■";
                        statusLbl.Content = "Mining (0 shares).";
                    });
                };
                miner.OnShare += (s, evt) =>
                {
                    statusLbl.Dispatcher.Invoke(() =>
                    {
                        shares++;
                        buttonLbl.Content = "■";
                        statusLbl.Content = $"Mining ({shares} shares).";
                    });
                };
                miner.Start(creds);
            }
            else
            {

                mining = false;
                miner.Stop();
                buttonLbl.Content = "▶";
                statusLbl.Content = "Click to start mining.";
            }

            var fadeIn = (Storyboard)FindResource("FadeIn");
            fadeIn.Begin(this);
        }

        private void buttonLbl_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as Label).Foreground = (Brush)FindResource("MinerHighlight");
        }

        private void buttonLbl_MouseLeave(object sender, MouseEventArgs e)
        {
            (sender as Label).Foreground = (Brush)FindResource("MinerGreen");
        }
    }
}
