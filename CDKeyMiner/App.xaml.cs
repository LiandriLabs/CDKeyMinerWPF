using Serilog;
using System;
using System.Diagnostics;
using System.Security.Permissions;
using System.Windows;

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

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        public App()
        {
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                    .MinimumLevel.Debug()
#else
                    .MinimumLevel.Information()
#endif
                    .WriteTo.File("cdkm.log", rollingInterval: RollingInterval.Day)
                    .CreateLogger();

            Log.Information("CDKeyMiner is starting...");

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            Log.Error(ex, "Unhandled exception");
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
        }
    }
}
