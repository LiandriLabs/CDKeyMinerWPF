using System;
using System.Collections.Generic;
using System.Globalization;
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

        public Dashboard()
        {
            InitializeComponent();
            creds = (Application.Current as App).Creds;
            WSHelper.Instance.OnBalance += OnBalance;
            startBal = (Application.Current as App).StartBalance;
            var balStr = startBal.ToString("F3", CultureInfo.InvariantCulture);
            balanceLbl.Content = $"Your balance: {balStr} CDKT";
        }

        private void OnBalance(object sender, double e)
        {
            this.Dispatcher.Invoke(() =>
            {
                var balStr = e.ToString("F3", CultureInfo.InvariantCulture);

                var elapsed = DateTime.UtcNow - startTime;
                if (elapsed.TotalMinutes >= 5)
                {
                    var deltaBal = e - startBal;
                    var est = ((24 * 60) / elapsed.TotalMinutes) * deltaBal;
                    var estStr = est.ToString("F1", CultureInfo.InvariantCulture);
                    balanceLbl.AnimatedUpdate($"Your balance: {balStr} CDKT (+{estStr} / 24h)");
                }
                else
                {
                    balanceLbl.AnimatedUpdate($"Your balance: {balStr} CDKT");
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
            (Application.Current.MainWindow as MainWindow).LogoutButton.Visibility = Visibility.Visible;
            algo = (Application.Current as App).Algo;
            //var sb = (Storyboard)FindResource("FadeIn");
            //sb.Begin(this);
        }

        private void Label_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!mining)
            {
                mining = true;
                buttonLbl.AnimatedUpdate("■");
                statusLbl.AnimatedUpdate("Starting miner...");
                miner = new Phoenix();
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
                    });
                };
                /*miner.OnShare += (s, evt) =>
                {
                    statusLbl.Dispatcher.Invoke(() =>
                    {
                        shares++;
                        buttonLbl.AnimatedUpdate("■");
                        statusLbl.AnimatedUpdate($"Mining {algo} ({shares} shares)");
                    });
                };*/
                miner.OnHashrate += (s, hr) =>
                {
                    statusLbl.Dispatcher.Invoke(() =>
                    {
                        buttonLbl.AnimatedUpdate("■");
                        statusLbl.AnimatedUpdate($"Mining {algo} ({hr})");
                    });
                };
                miner.Start(creds);
            }
            else
            {

                mining = false;
                miner.Stop();
                buttonLbl.AnimatedUpdate("▶");
                statusLbl.AnimatedUpdate("Click the button to start mining.");
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
    }
}
