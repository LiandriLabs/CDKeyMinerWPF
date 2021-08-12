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
using System.Management;
using Serilog;
using System.Windows.Media.Animation;
using System.Diagnostics;
using System.IO;

namespace CDKeyMiner
{
    /// <summary>
    /// Interaction logic for AV.xaml
    /// </summary>
    public partial class AV : Page
    {
        string libPath;

        public AV()
        {
            InitializeComponent();
            libPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib");
        }

        private void AVExcludeBtn_Click(object sender, RoutedEventArgs e)
        {
            var elevated = new ProcessStartInfo("powershell")
            {
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas",
                Arguments = " -Command Add-MpPreference -ExclusionPath '" + libPath + "'"
            };
            Process.Start(elevated);

            Properties.Settings.Default.AVExclusion = libPath;
            Properties.Settings.Default.Save();
            NavigationService.Navigate(new Download());
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Information("Page loaded: AV");
            if (Properties.Settings.Default.AVExclusion == libPath)
            {
                NavigationService.Navigate(new Download());
            }
            else
            {
                try
                {
                    ManagementObjectSearcher wmiData = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT * FROM AntiVirusProduct");
                    ManagementObjectCollection data = wmiData.Get();

                    bool found = false;
                    foreach (ManagementObject virusChecker in data)
                    {
                        var name = virusChecker["displayName"] as string;
                        if (name == "Windows Defender")
                        {
                            found = true;
                        }
                    }

                    if (found)
                    {
                        var sb = (Storyboard)FindResource("FadeIn");
                        sb.Begin(this);
                    }
                    else
                    {
                        NavigationService.Navigate(new Download());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error detecting AV product");
                    NavigationService.Navigate(new Download());
                }
            }
        }

        private void SkipBtn_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AVExclusion = libPath;
            Properties.Settings.Default.Save();
            NavigationService.Navigate(new Download());
        }
    }
}
