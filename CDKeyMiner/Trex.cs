using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDKeyMiner
{
    class Trex : IMiner
    {
        private string libPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lib");
        private Process trexProc;

        public Trex()
        {

        }

        public event EventHandler OnAuthorized;
        public event EventHandler OnMining;
        public event EventHandler OnShare;
        public event EventHandler<MinerError> OnError;

        public void Start(Credentials credentials)
        {
            var trexPath = Path.Combine(libPath, "t-rex.exe");
            if (!File.Exists(trexPath))
            {
                OnError?.Invoke(this, MinerError.ExeNotFound);
            }

            Stop();

            trexProc = new Process();

            trexProc.StartInfo.UseShellExecute = false;
            trexProc.StartInfo.RedirectStandardOutput = true;
            trexProc.StartInfo.RedirectStandardError = true;

            trexProc.OutputDataReceived += new DataReceivedEventHandler((sender, e) => {
                if (e.Data == null)
                {
                    return;
                }

                if (e.Data.Contains("Authorized successfully"))
                {
                    OnAuthorized?.Invoke(this, null);
                }
                else if (e.Data.Contains("using kernel #"))
                {
                    OnMining?.Invoke(this, null);
                }
                else if (e.Data.Contains("[ OK ]"))
                {
                    OnShare?.Invoke(this, null);
                }
                else if (e.Data.Contains("ERROR: No connection"))
                {
                    OnError?.Invoke(this, MinerError.ConnectionError);
                }
            });

            trexProc.ErrorDataReceived += new DataReceivedEventHandler((sender, e) => {
                OnError?.Invoke(this, MinerError.UnknownError);
            });

            trexProc.StartInfo.CreateNoWindow = true;
            trexProc.StartInfo.FileName = trexPath;
            trexProc.StartInfo.Arguments = $"-a ethash -o stratum+tcp://192.168.1.3:9000 -u {credentials.Username} -p x -w rig0";
            trexProc.Start();
            trexProc.BeginOutputReadLine();
            trexProc.BeginErrorReadLine();
        }

        public void Stop()
        {
            if (trexProc != null && !trexProc.HasExited)
            {
                //trexProc.Exited += (s, e) =>
                //{
                //    OnError?.Invoke(this, MinerError.HasStopped);
                //};
                trexProc.Kill();
                trexProc = null;
                OnError?.Invoke(this, MinerError.HasStopped);
            }
        }
    }
}
