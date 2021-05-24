﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace CDKeyMiner
{
    class Phoenix : IMiner
    {
        private string libPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib");
        private Process phoenixProc;

        public Phoenix()
        {

        }

        public event EventHandler OnAuthorized;
        public event EventHandler OnMining;
        public event EventHandler OnShare;
        public event EventHandler<MinerError> OnError;

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
            phoenixProc.StartInfo.Arguments = $"-pool pool.cdkeyminer.com:9000 -wal {credentials.Username} -proto 2 -coin eth -stales 0 -rate 0 -cdm 0 -gsi 0 -log 1";
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