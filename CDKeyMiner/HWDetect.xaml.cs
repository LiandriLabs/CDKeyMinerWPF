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

        private bool NeedsComputeMode(RegistryKey key)
        {
            var gpuProv = (string)key.GetValue("ProviderName");
            var driverDesc = (string)key.GetValue("DriverDesc");
            if (gpuProv.Contains("Advanced Micro Devices") || driverDesc.Contains("Radeon"))
            {
                var lp = key.GetValue("KMD_EnableInternalLargePage");
                if (lp == null || (int)lp != 2)
                {
                    return true;
                }
            }
            return false;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("Detecting hardware");

            var sb = (Storyboard)FindResource("FadeIn");
            sb.Begin(this);

            try
            {
                var over5GB = false;
                var over3GB = false;
                var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
                foreach (var gpu in key.GetSubKeyNames().Where(n => n.StartsWith("0")))
                {
                    try
                    {
                        var gpuKey = key.OpenSubKey(gpu);
                        var val = (long)gpuKey.GetValue("HardwareInformation.qwMemorySize");
                        if (val > 5368709120)
                        {
                            over5GB = true;
                            app.GPU = (string)gpuKey.GetValue("DriverDesc");
                            needsComputeMode = needsComputeMode || NeedsComputeMode(gpuKey);
                        }
                        else if (val > 2684354560)
                        {
                            over3GB = true;
                            app.GPU = (string)gpuKey.GetValue("DriverDesc");
                            needsComputeMode = needsComputeMode || NeedsComputeMode(gpuKey);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Couldn't assess memory size for GPU " + gpu);
                    }
                }

                if (!over5GB && !over3GB)
                {
                    Log.Error("Couldn't find GPU with at least 3 GB VRAM");
                    Status.Content = "Sorry, we couldn't find a GPU with at least 3 GB VRAM";
                    return;
                }

                if (over5GB)
                {
                    app.Algo = Algo.ETH;
                }
                else
                {
                    app.Algo = Algo.ETC;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Hardware detection has failed, mining ETC");
                app.GPU = "Detection failed, contact support";
                app.Algo = Algo.ETC;
            }

            if (needsComputeMode)
            {
                Status.Content = "You have an AMD GPU, but compute mode is not enabled.";
                CompModeBtn.Visibility = Visibility.Visible;
                SkipBtn.Visibility = Visibility.Visible;
            }
            else
            {
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
