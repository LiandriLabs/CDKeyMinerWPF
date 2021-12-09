using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Globalization;

namespace CDKeyMiner
{
    public class NBMiner : IMiner
    {
        private string libPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib");
        private Process nbminerProc;
        private Credentials lastCreds;
        private bool parsingSummary = false;
        Regex hrRx = new Regex(@".*Total:\s+(?<hr>\d+.\d+\s+M).*", RegexOptions.Compiled);
        private int maxTemp = -1;
        private int sumIncorrect = 0;

        public NBMiner()
        {

        }

        public event EventHandler OnAuthorized;
        public event EventHandler OnMining;
        public event EventHandler OnShare;
        public event EventHandler<MinerError> OnError;
        public event EventHandler<string> OnHashrate;
        public event EventHandler<int> OnTemperature;
        public event EventHandler<int> OnIncorrectShares;

        public void Start(Credentials credentials)
        {
            Log.Information("Starting NBMiner.");
            lastCreds = credentials;

            var minerExePath = Path.Combine(libPath, "nbminer.exe");
            if (!File.Exists(minerExePath))
            {
                Log.Error("NBMiner not found in {0}", minerExePath);
                OnError?.Invoke(this, MinerError.ExeNotFound);
                return;
            }

            Stop();

            nbminerProc = new Process();

            nbminerProc.StartInfo.UseShellExecute = false;
            nbminerProc.StartInfo.RedirectStandardOutput = true;
            nbminerProc.StartInfo.RedirectStandardError = true;
            nbminerProc.StartInfo.CreateNoWindow = true;
            nbminerProc.StartInfo.FileName = minerExePath;

            nbminerProc.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            {
                Log.Debug(e.Data);
            });

            nbminerProc.ErrorDataReceived += new DataReceivedEventHandler((sender, e) => {
                Log.Debug(e.Data);
                if (e.Data == null)
                {
                    return;
                }

                if (!parsingSummary)
                {
                    if (e.Data.Contains("Login succeeded"))
                    {
                        OnAuthorized?.Invoke(this, null);
                    }
                    else if (e.Data.Contains("DAG - Verification ok"))
                    {
                        OnMining?.Invoke(this, null);
                    }
                    else if (e.Data.Contains("Share accepted"))
                    {
                        OnShare?.Invoke(this, null);
                    }
                    else if (e.Data.Contains("Socket error") || e.Data.Contains("Failed to establish connection"))
                    {
                        Log.Error("NBMiner connection error: {0}", e.Data);
                        OnError?.Invoke(this, MinerError.ConnectionError);
                    }
                    else if (e.Data.Contains("out of memory"))
                    {
                        Log.Error("NBMiner out of memory error");
                        OnError?.Invoke(this, MinerError.OutOfMemory);
                    }
                    else if (e.Data.Contains("Summary"))
                    {
                        parsingSummary = true;
                    }
                }
                else // parsing summary
                {
                    if (e.Data.Contains("Total"))
                    {
                        parsingSummary = false;
                        try
                        {
                            var matches = hrRx.Matches(e.Data);
                            if (matches.Count > 0)
                            {
                                OnHashrate?.Invoke(this, matches[0].Groups["hr"].Value);
                            }
                            OnIncorrectShares?.Invoke(this, sumIncorrect);
                            OnTemperature?.Invoke(this, maxTemp);
                            sumIncorrect = 0;
                            maxTemp = -1;
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Error reporting hashrate", ex);
                        }
                    }
                    else
                    {
                        var cols = e.Data.Split('|');
                        if (cols.Length > 9)
                        {
                            try
                            {
                                var inv = int.Parse(cols[7].Trim());
                                sumIncorrect += inv;

                                var tmp = int.Parse(cols[9].Trim());
                                if (tmp > maxTemp)
                                {
                                    maxTemp = tmp;
                                }
                            }
                            catch { }
                        }
                    }
                }
            });

            var algo = (Application.Current as App).Algo;
            if (algo == Algo.ETH)
            {
                nbminerProc.StartInfo.Arguments = $"-a ethash -o stratum+ssl://app.cdkeyminer.com:10000 -strict-ssl -u {credentials.Username} -log -api 127.0.0.1:22333";
            }
            else if (algo == Algo.ETC)
            {
                nbminerProc.StartInfo.Arguments = $"-a etchash -o stratum+ssl://app.cdkeyminer.com:10001 -strict-ssl -u {credentials.Username} -log -api 127.0.0.1:22333";
            }
            else
            {
                Log.Error("This should never happen");
                Application.Current.Shutdown();
            }

            if (Properties.Settings.Default.EcoMode)
            {
                nbminerProc.StartInfo.Arguments += " -i 75";
            }

            Log.Information("Arguments: {0}", nbminerProc.StartInfo.Arguments);
            nbminerProc.Start();
            nbminerProc.BeginOutputReadLine();
            nbminerProc.BeginErrorReadLine();
        }

        public void Stop()
        {
            Log.Information("Stopping NBMiner.");
            parsingSummary = false;
            if (nbminerProc != null && !nbminerProc.HasExited)
            {
                nbminerProc.Kill();
            } 
            Process[] localByName = Process.GetProcessesByName("nbminer");
            foreach (var p in localByName)
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
            nbminerProc = null;
            OnError?.Invoke(this, MinerError.HasStopped);
        }

        public void Restart()
        {
            Stop();
            Start(lastCreds);
        }
    }
}
