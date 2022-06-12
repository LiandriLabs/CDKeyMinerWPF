using Serilog;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Permissions;
using System.Windows;
using System.IO;

namespace CDKeyMiner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public Credentials Creds;
        public Algo Algo;
        public string GPU;
        public double StartBalance;
        public Dashboard DashboardPage;
        public Info InfoPage;
        public string Theme;
        public IMiner Miner;

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        public App()
        {
            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            string logsFolder = Path.Combine(appFolder, "Logs");

            // move old logs from root folder to subfolder
            try
            {
                foreach (var f in Directory.GetFiles(appFolder))
                {
                    try
                    {
                        var finfo = new FileInfo(f);
                        if ((finfo.Name.StartsWith("cdkm") && finfo.Extension == ".log")
                            || (finfo.Name.StartsWith("log") && finfo.Extension == ".txt"))
                        {
                            File.Move(f, Path.Combine(logsFolder, finfo.Name));
                        }
                    }
                    catch { }
                }
            }
            catch { }

            Log.Logger = new LoggerConfiguration()
#if DEBUG
                    .MinimumLevel.Debug()
#else
                    .MinimumLevel.Information()
#endif
                    .WriteTo.File("Logs\\cdkm.log", rollingInterval: RollingInterval.Day)
                    .WriteTo.Sink(SerilogEventSink.Instance)
                    .CreateLogger();

            Log.Information("CDKeyMiner {Version} is starting...", Assembly.GetExecutingAssembly().GetName().Version);

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                foreach (var f in Directory.GetFiles(logsFolder))
                {
                    try
                    {
                        var finfo = new FileInfo(f);
                        if (finfo.LastWriteTime < DateTime.Now.AddDays(-7))
                        {
                            File.Delete(f);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Couldn't check log file {File}", f);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Couldn't clear old logs");
            }
        }

        public bool SetMiningAlgorithm(string[] recommendations)
        {
            var found = false;
            foreach (var algo in recommendations)
            {
                if (algo == "ETH")
                {
                    found = true;
                    this.Algo = Algo.ETH;
                    break;
                }
                else if (algo == "ETC")
                {
                    found = true;
                    this.Algo = Algo.ETC;
                    break;
                }
            }

            return found;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            Log.Error(ex, "Unhandled exception");

            Process[] phoenixProcs = Process.GetProcessesByName("PhoenixMiner");
            foreach (var p in phoenixProcs)
            {
                try
                {
                    if (!p.HasExited)
                    {
                        p.Kill();
                    }
                }
                catch { }
            }

            Process[] nbProcs = Process.GetProcessesByName("nbminer");
            foreach (var p in nbProcs)
            {
                try
                {
                    if (!p.HasExited)
                    {
                        p.Kill();
                    }
                }
                catch { }
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Log.Information("Arguments: {Args}", e.Args);
            if (e.Args.Length == 1)
            {
                try
                {
                    Process proc = Process.GetProcessById(int.Parse(e.Args[0]));
                    Log.Information("Waiting for process with PID {PID} to exit", e.Args[0]);
                    proc.WaitForExit();
                }
                catch (ArgumentException ex)
                {
                    Log.Information("Process is already closed");
                }
            }

            Theme = CDKeyMiner.Properties.Settings.Default.Theme;

            if (Theme == "Light")
            {
                Resources.Clear();
                Resources.MergedDictionaries.Clear();
                Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("ColorsLight.xaml", UriKind.Relative) });
                Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("Skin.xaml", UriKind.Relative) });
            }
            else if (Theme == "Glass")
            {
                Resources.Clear();
                Resources.MergedDictionaries.Clear();
                Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("ColorsGlass.xaml", UriKind.Relative) });
                Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("Skin.xaml", UriKind.Relative) });
            }
        }
    }
}
