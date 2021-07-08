using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Serilog;

namespace CDKeyMiner
{
    class Phoenix : IMiner
    {
        private string libPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib");
        private Process phoenixProc;
        Regex tempRx = new Regex(@"^GPU.: (?<temp>\d+)C.*", RegexOptions.Compiled);
        bool hasNvidiaGPU = (Application.Current as App).GPU.IndexOf("nvidia", StringComparison.InvariantCultureIgnoreCase) != -1;

        public Phoenix()
        {

        }

        public event EventHandler OnAuthorized;
        public event EventHandler OnMining;
        public event EventHandler OnShare;
        public event EventHandler<MinerError> OnError;
        public event EventHandler<string> OnHashrate;
        public event EventHandler<int> OnTemperature;

        public void Start(Credentials credentials)
        {
            Log.Information("Starting Phoenix miner.");
            var minerExePath = Path.Combine(libPath, "PhoenixMiner.exe");
            if (!File.Exists(minerExePath))
            {
                Log.Error("Phoenix not found in {0}", minerExePath);
                OnError?.Invoke(this, MinerError.ExeNotFound);
                return;
            }

            Stop();

            phoenixProc = new Process();

            phoenixProc.StartInfo.UseShellExecute = false;
            phoenixProc.StartInfo.RedirectStandardOutput = true;
            phoenixProc.StartInfo.RedirectStandardError = true;

            phoenixProc.OutputDataReceived += new DataReceivedEventHandler((sender, e) => {
                Log.Debug(e.Data);
                if (e.Data == null)
                {
                    return;
                }

                if (e.Data.Contains("Eth: Connected to ethash pool"))
                {
                    OnAuthorized?.Invoke(this, null);
                }
                else if (e.Data.Contains("GPU1: DAG generated"))
                {
                    OnMining?.Invoke(this, null);
                }
                else if (e.Data.Contains("Eth: Share accepted"))
                {
                    OnShare?.Invoke(this, null);
                }
                else if (e.Data.Contains("Eth: Can't resolve host") ||
                         e.Data.Contains("Eth: Could not connect") ||
                         e.Data.Contains("Eth: Connection closed"))
                {
                    Log.Error("Phoenix connection error.");
                    OnError?.Invoke(this, MinerError.ConnectionError);
                }
                else if (e.Data.Contains("Eth speed:"))
                {
                    var start = e.Data.IndexOf("Eth speed:") + 11;
                    var end = e.Data.IndexOf(",");
                    var hashrate = e.Data.Substring(start, end - start);
                    OnHashrate?.Invoke(this, hashrate);
                }
                else if (tempRx.IsMatch(e.Data))
                {
                    try
                    {
                        var matches = tempRx.Matches(e.Data);
                        var temp = int.Parse(matches[0].Groups["temp"].Value);
                        OnTemperature?.Invoke(this, temp);
                    }
                    catch { }
                }
            });

            phoenixProc.ErrorDataReceived += new DataReceivedEventHandler((sender, e) => {
                if (e.Data != null)
                {
                    Log.Error("Error from Phoenix: {0}", e.Data);
                }
                OnError?.Invoke(this, MinerError.UnknownError);
            });

            phoenixProc.StartInfo.CreateNoWindow = true;
            phoenixProc.StartInfo.FileName = minerExePath;
            var algo = (Application.Current as App).Algo;
            if (algo == Algo.ETH)
            {
                phoenixProc.StartInfo.Arguments = $"-pool pool.cdkeyminer.com:9000 -wal {credentials.Username} -proto 2 -coin eth -stales 0 -rate 0 -cdm 0 -gsi 0 -log 1";
            }
            else if (algo == Algo.ETC)
            {
                phoenixProc.StartInfo.Arguments = $"-pool pool.cdkeyminer.com:9001 -wal {credentials.Username} -proto 2 -coin etc -stales 0 -rate 0 -cdm 0 -gsi 0 -log 1";
            }
            else
            {
                Log.Error("This should never happen");
                Application.Current.Shutdown();
            }

            if (hasNvidiaGPU)
            {
                phoenixProc.StartInfo.Arguments += " -nvidia";
            }

            phoenixProc.Start();
            phoenixProc.BeginOutputReadLine();
            phoenixProc.BeginErrorReadLine();
        }

        public void Stop()
        { 
            if (phoenixProc != null && !phoenixProc.HasExited)
            {
                Log.Information("Stopping Phoenix miner.");
                phoenixProc.Kill();
                phoenixProc = null;
                OnError?.Invoke(this, MinerError.HasStopped);
            }
        }
    }
}
