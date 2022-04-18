using Microsoft.Win32;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using Newtonsoft.Json;

namespace CDKeyMiner
{
    /// <summary>
    /// Interaction logic for HWDetect.xaml
    /// </summary>
    public partial class HWDetect : Page
    {
        private App app = (App)Application.Current;
        private bool needsComputeMode = false;

        public HWDetect()
        {
            InitializeComponent();
        }

        private bool NeedsComputeMode(string forGPU)
        {
            RegistryKey key;
            try
            {
                key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Cannot open GPU registry key");
                return false;
            }

            foreach (var gpu in key.GetSubKeyNames().Where(n => n.StartsWith("0")))
            {
                try
                {
                    var gpuKey = key.OpenSubKey(gpu);
                    var gpuProv = (string)gpuKey.GetValue("ProviderName");
                    var driverDesc = (string)gpuKey.GetValue("DriverDesc");
                    if (driverDesc != forGPU)
                    {
                        continue;
                    }
                    if (gpuProv.Contains("Advanced Micro Devices") || driverDesc.Contains("Radeon"))
                    {
                        var lp = gpuKey.GetValue("KMD_EnableInternalLargePage");
                        if (lp == null || (int)lp != 2)
                        {
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Cannot enumerate GPU " + gpu);
                }
            }

            return false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("Page loaded: HWDetect");

            var sb = (Storyboard)FindResource("FadeIn");
            sb.Begin(this);

            try
            {
                var report = new Hardware.HWReport();
                var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
                foreach (var gpu in key.GetSubKeyNames().Where(n => n.StartsWith("0")))
                {
                    try
                    {
                        var gpuKey = key.OpenSubKey(gpu);
                        var memBytes = (long)gpuKey.GetValue("HardwareInformation.qwMemorySize");
                        var gpuProv = (string)gpuKey.GetValue("ProviderName");
                        var driverDesc = (string)gpuKey.GetValue("DriverDesc");
                        var dacType = (string)gpuKey.GetValue("HardwareInformation.DacType");
                        var driverDate = (string)gpuKey.GetValue("DriverDate");

                        report.GPU.Add(new Hardware.GPUInfo
                        {
                            Description = driverDesc,
                            Provider = gpuProv,
                            MemoryBytes = memBytes,
                            DacType = dacType,
                            DriverDate = driverDate
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Couldn't assess memory size for GPU " + gpu);
                    }
                }

                WSHelper.Instance.OnRecommend += Server_OnRecommend;
                WSHelper.Instance.ReportHardware(report);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Hardware detection has failed");
                Status.Content = "Detection failed, contact support";
            }
        }

        private void Server_OnRecommend(object sender, Hardware.HWResponse e)
        {
            WSHelper.Instance.OnRecommend -= Server_OnRecommend;
            if (e.Algos.Length == 0)
            {
                Log.Error("Couldn't find suitable hardware");
                Status.Content = "Couldn't find suitable hardware, please contact support";
                return;
            }

            var found = false;
            foreach (var algo in e.Algos)
            {
                if (algo == "ETH")
                {
                    found = true;
                    app.Algo = Algo.ETH;
                    break;
                }
                else if (algo == "ETC")
                {
                    found = true;
                    app.Algo = Algo.ETC;
                    break;
                }
            }

            if (!found)
            {
                Log.Error("No supported algorithm");
                Status.Content = "Please update your client";
                return;
            }

            app.GPU = e.BestGPU;

            if (NeedsComputeMode(e.BestGPU))
            {
                Status.Content = "You have an AMD GPU, but compute mode is not enabled.";
                CompModeBtn.Visibility = Visibility.Visible;
                SkipBtn.Visibility = Visibility.Visible;
            }
            else
            {
                // continue
                NavigationService.Navigate(new AV());
            }
        }

        private async void CompModeBtn_Click(object sender, RoutedEventArgs e)
        {
            int res = await RunProcessAsync("EnableComputeMode.exe");
            if (res == 0)
            {
                CompModeBtn.Visibility = Visibility.Collapsed;
                SkipBtn.Visibility = Visibility.Collapsed;
                Status.Content = "Success, please reboot your PC.";
            }
            else
            {
                CompModeBtn.Visibility = Visibility.Collapsed;
                SkipBtn.Visibility = Visibility.Collapsed;
                Status.Content = "Failed, please enable manually.";
            }
        }

        private void SkipBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AV());
        }

        static Task<int> RunProcessAsync(string fileName)
        {
            var tcs = new TaskCompletionSource<int>();

            var process = new Process
            {
                StartInfo = { FileName = fileName },
                EnableRaisingEvents = true
            };

            process.Exited += (sender, args) =>
            {
                tcs.SetResult(process.ExitCode);
                process.Dispose();
            };

            process.Start();

            return tcs.Task;
        }
    }
}
