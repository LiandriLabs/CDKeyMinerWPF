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
                var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
                foreach (var gpu in key.GetSubKeyNames().Where(n => n.StartsWith("0")))
                {
                    try
                    {
                        var val = (long)key.OpenSubKey(gpu).GetValue("HardwareInformation.qwMemorySize");
                        if (val > 5368709120)
                        {
                            over5GB = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Couldn't access memory size");
                    }
                }

                if (!over5GB)
                {
                    Log.Error("Couldn't find GPU with at least 6 GB VRAM");
                    Status.Content = "Sorry, we couldn't find a GPU with at least 6 GB VRAM";
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Hardware detection has failed, will continue to download");
            }

            NavigationService.Navigate(new Download());
        }
    }
}
