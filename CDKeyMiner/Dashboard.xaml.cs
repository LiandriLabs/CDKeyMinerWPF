﻿using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
using System.Windows.Threading;

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
        private Algo algo;
        private DateTime startTime;
        private double startBal;
        private double bal;
        private MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
        private App app = (App)Application.Current;
        private bool notificationSeen = false;

        public Dashboard()
        {
            InitializeComponent();
            creds = app.Creds;
            WSHelper.Instance.OnBalance += OnBalance;
            startBal = app.StartBalance;
            bal = startBal;
            var balStr = startBal.ToString("F3", CultureInfo.InvariantCulture);
            mainWindow.balanceLbl.Content = $"Balance: {balStr} CDKT";
            algo = app.Algo;
        }

        private void OnBalance(object sender, double e)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (e < bal)
                {
                    // user bought something
                    var purchase = bal - e;
                    startBal -= purchase;
                }

                bal = e;
                var balStr = bal.ToString("F3", CultureInfo.InvariantCulture);

                var elapsed = DateTime.UtcNow - startTime;
                if (mining && elapsed.TotalMinutes >= 10)
                {
                    var deltaBal = bal - startBal;
                    var est = ((24 * 60) / elapsed.TotalMinutes) * deltaBal;
                    var estStr = est.ToString("F1", CultureInfo.InvariantCulture);
                    mainWindow.balanceLbl.Content = $"Balance: {balStr} CDKT (+{estStr} / 24h)";
                }
                else
                {
                    mainWindow.balanceLbl.Content = $"Balance: {balStr} CDKT";
                }
            });
        }

        ~Dashboard()
        {
            StopMiner();
            WSHelper.Instance.Disconnect();
        }

        public void StopMiner()
        {
            if (miner != null)
            {
                miner.Stop();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("Page loaded: Dashboard");
            (Application.Current.MainWindow as MainWindow).SettingsButton.Visibility = Visibility.Visible;
            app.InfoPage.UserLabel.Content = creds.Username;
            app.InfoPage.GPULabel.Content = app.GPU;
            var appVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            app.InfoPage.VersionLabel.Content = appVersion;
            var upd = Updater.Instance.CheckForUpdates();
            if (upd)
            {
                if (!notificationSeen)
                {
                    InfoButton.Content = "● Information";
                }
                app.InfoPage.VersionLabel.Content = appVersion + " (latest: " + Updater.Instance.NewVersion + ")";
                app.InfoPage.UpdateButton.IsEnabled = true;
            }
        }

        private void Label_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!mining)
            {
                mining = true;
                buttonLbl.AnimatedUpdate("■");
                statusLbl.AnimatedUpdate("Starting miner...");
                miner = Phoenix.Instance;
                miner.OnError += (s, err) =>
                {
                    if (err == MinerError.ExeNotFound)
                    {
                        statusLbl.Dispatcher.Invoke(() =>
                        {
                            buttonLbl.AnimatedUpdate("⚠");
                            statusLbl.AnimatedUpdate("Cannot find miner EXE");
                        });
                    }
                    else if (err == MinerError.ConnectionError)
                    {
                        statusLbl.Dispatcher.Invoke(() =>
                        {
                            buttonLbl.AnimatedUpdate("⚠");
                            statusLbl.AnimatedUpdate("Connection error - retrying");
                        });
                    }
                };
                miner.OnAuthorized += (s, evt) =>
                {
                    statusLbl.Dispatcher.Invoke(() =>
                    {
                        buttonLbl.AnimatedUpdate("■");
                        statusLbl.AnimatedUpdate("Miner connected, please wait...");
                    });
                };
                miner.OnMining += (s, evt) =>
                {
                    statusLbl.Dispatcher.Invoke(() =>
                    {
                        buttonLbl.AnimatedUpdate("■");
                        statusLbl.AnimatedUpdate($"Mining {algo}");
                        startTime = DateTime.UtcNow;
                        startBal = bal;
                    });
                };
                miner.OnHashrate += (s, hr) =>
                {
                    statusLbl.Dispatcher.Invoke(() =>
                    {
                        buttonLbl.Content = "■";
                        statusLbl.Content = $"Mining {algo} ({hr})";
                        try
                        {
                            var hrNumStr = hr.Substring(0, hr.IndexOf(' '));
                            var hrNum = double.Parse(hrNumStr, CultureInfo.InvariantCulture);
                            WSHelper.Instance.ReportHashrate(hrNum);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Report hash rate error");
                        }
                    });
                };
                miner.OnTemperature += (s, t) =>
                {
                    try
                    {
                        WSHelper.Instance.ReportTemperature(t);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Report temperature error");
                    }

                    app.InfoPage.TempLabel.Dispatcher.Invoke(() =>
                    {
                        app.InfoPage.TempLabel.Content = t.ToString() + "°C";
                        if (t >= 85)
                        {
                            app.InfoPage.TempLabel.Foreground = Application.Current.TryFindResource("MinerOrange") as SolidColorBrush;
                        }
                        else
                        {
                            app.InfoPage.TempLabel.Foreground = Application.Current.TryFindResource("MinerGreen") as SolidColorBrush;
                        }
                    });

                    if (t >= 90)
                    {
                        statusLbl.Dispatcher.Invoke(() =>
                        {
                            app.InfoPage.TempLabel.Content = "N/A";
                            app.InfoPage.TempLabel.Foreground = Application.Current.TryFindResource("MinerGreen") as SolidColorBrush;
                            mining = false;
                            miner.Stop();
                            buttonLbl.AnimatedUpdate("⚠");
                            statusLbl.AnimatedUpdate("Stopped because temperature exceeded 90°C");
                        });
                    }
                };
                miner.OnIncorrectShares += (s, sh) =>
                {
                    if (sh > 0)
                    {
                        statusLbl.Dispatcher.Invoke(() =>
                        {
                            statusLbl.AnimatedUpdate($"Your GPU submitted {sh} incorrect share(s)");
                        });
                    }
                };
                miner.Start(creds);
            }
            else
            {
                Dispatcher.Invoke(() =>
                {
                    app.InfoPage.TempLabel.Content = "N/A";
                    app.InfoPage.TempLabel.Foreground = Application.Current.TryFindResource("MinerGreen") as SolidColorBrush;
                    mining = false;
                    miner.Stop();
                    buttonLbl.AnimatedUpdate("▶");
                    statusLbl.AnimatedUpdate("Click the button to start mining.");
                });    
            }
        }

        private void buttonLbl_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as Label).Foreground = (Brush)FindResource("MinerHighlight");
        }

        private void buttonLbl_MouseLeave(object sender, MouseEventArgs e)
        {
            (sender as Label).Foreground = (Brush)FindResource("MinerGreen");
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            InfoButton.Content = "Information";
            notificationSeen = true;
            NavigationService.Navigate(app.InfoPage);
        }
    }
}
