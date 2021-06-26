using Serilog;
using System;
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
        public double StartBalance;
        public Dashboard DashboardPage;
        public Info InfoPage;

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        public App()
        {
            Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
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
    }
}
