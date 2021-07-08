using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EnableComputeMode
{
    class Program
    {
        static void Main(string[] args)
        {
            bool success = false;
            try
            {
                var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
                foreach (var gpu in key.GetSubKeyNames().Where(n => n.StartsWith("0")))
                {
                    var gpuKey = key.OpenSubKey(gpu, true);
                    try
                    {
                        var gpuProv = (string)gpuKey.GetValue("ProviderName");
                        var driverDesc = (string)gpuKey.GetValue("DriverDesc");
                        if (gpuProv.Contains("Advanced Micro Devices") || driverDesc.Contains("Radeon"))
                        {
                            if (args.Contains("disable"))
                            {
                                gpuKey.SetValue("KMD_EnableInternalLargePage", 0);
                                success = true;
                            }
                            else
                            {
                                gpuKey.SetValue("KMD_EnableInternalLargePage", 2);
                                success = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Thread.Sleep(5000);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Thread.Sleep(5000);
                Environment.Exit(-1);
            }

            if (!success)
            {
                Environment.Exit(-1);
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }
}
