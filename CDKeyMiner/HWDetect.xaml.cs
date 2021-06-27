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
using System.Windows.Shapes;
using System.Management;
using Microsoft.Win32;
using Serilog;
using System.Windows.Media.Animation;

namespace CDKeyMiner
{
    /// <summary>
    /// Interaction logic for HWDetect.xaml
    /// </summary>
    public partial class HWDetect : Page
    {
        private App app = (App)Application.Current;

        public HWDetect()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("Detecting hardware");

            var sb = (Storyboard)FindResource("FadeIn");
            sb.Begin(this);

            /*var dedicatedGPU = false;
            using (var searcher = new ManagementObjectSearcher("select * from Win32_VideoController"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var DACType = (string)obj["AdapterDACType"];
                    if (DACType != "Internal")
                    {
                        dedicatedGPU = true;
                        break;
                    }
                }
            }

            if (!dedicatedGPU)
            {
                Log.Error("Couldn't find dedicated GPU");
                Status.Content = "Sorry, we couldn't find a dedicated GPU";
                return;
            }*/

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
                            app.GPU = (string)gpuKey.GetValue("HardwareInformation.AdapterString");
                            break;
                        }
                        else if (val > 2684354560)
                        {
                            over3GB = true;
                            app.GPU = (string)gpuKey.GetValue("HardwareInformation.AdapterString");
                            break;
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
                    (Application.Current as App).Algo = Algo.ETH;
                }
                else
                {
                    (Application.Current as App).Algo = Algo.ETC;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Hardware detection has failed, mining ETC");
                (Application.Current as App).GPU = "Detection failed, contact support";
                (Application.Current as App).Algo = Algo.ETC;
            }

            NavigationService.Navigate(new Download());
        }
    }
}
