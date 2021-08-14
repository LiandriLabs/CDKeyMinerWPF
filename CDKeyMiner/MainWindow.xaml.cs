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
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Serilog;

namespace CDKeyMiner
{
    internal enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
        ACCENT_INVALID_STATE = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;
        public uint AccentFlags;
        public uint GradientColor;
        public uint AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    internal enum WindowCompositionAttribute
    {
        // ...
        WCA_ACCENT_POLICY = 19
        // ...
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

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
                    try
                    {
                        mainWindow.Margin = new Thickness(0);
                        mainWindow.Effect = null;
                        this.Width -= 30;
                        this.Height -= 30;
                        var windowHelper = new WindowInteropHelper(this);
                        var accent = new AccentPolicy();
                        accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
                        var accentStructSize = Marshal.SizeOf(accent);
                        var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                        Marshal.StructureToPtr(accent, accentPtr, false);
                        var data = new WindowCompositionAttributeData();
                        data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
                        data.SizeOfData = accentStructSize;
                        data.Data = accentPtr;
                        SetWindowCompositionAttribute(windowHelper.Handle, ref data);
                        Marshal.FreeHGlobal(accentPtr);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Couldn't enable glass effect");
                    }
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

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LogoutButton.Visibility = Visibility.Collapsed;
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

        private void AppMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cm = this.FindResource("AppMenu") as ContextMenu;
            cm.PlacementTarget = sender as Image;
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
