using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CDKeyMiner
{
    public class WSHelper
    {
        private static WSHelper inst = null;
        private const string URL = "wss://app.cdkeyminer.com/socket.io/?EIO=4&transport=websocket";   
        private ClientWebSocket ws;
        private string jwt;
        private DispatcherTimer pingTimer = new DispatcherTimer();
        private CancellationTokenSource cts;

        private WSHelper()
        {
            pingTimer.Tick += PingTimer_Tick;
            pingTimer.Interval = new TimeSpan(0, 0, 35);
        }

        private void PingTimer_Tick(object sender, EventArgs e)
        {
            Log.Error("Ping timeout, reconnecting");
            cts.Cancel();
            pingTimer.Stop();
            ws.Abort();
            ws.Dispose();
            Resume(this.jwt);
        }

        public static WSHelper Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new WSHelper();
                }
                return inst;
            }
        }

        public event EventHandler OnLoggedIn;
        public event EventHandler OnLoginFailed;
        public event EventHandler<double> OnBalance;
        public event EventHandler<string> OnError;

        private async Task Send(ClientWebSocket socket, string data)
        {
            Log.Debug("WS Send: {Data}", data);
            await socket.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(data)),
                WebSocketMessageType.Text,
                true,
                cts.Token);
        }

        private async Task<string> Receive(ClientWebSocket socket)
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);
            WebSocketReceiveResult result;
            using (var ms = new MemoryStream())
            {
                do
                {
                    result = await socket.ReceiveAsync(buffer, cts.Token);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                } while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(ms, Encoding.UTF8))
                {
                    var msg = reader.ReadToEnd();
                    Log.Debug("WS Receive: {Data}", msg);
                    return msg;
                }
            }
        }

        private async Task<bool> Connect()
        {
            while (true)
            {
                try
                {
                    Log.Information("Connecting to WebSocket server");
                    cts = new CancellationTokenSource();
                    ws = new ClientWebSocket();
                    await ws.ConnectAsync(new Uri(URL), cts.Token);
                    Log.Information("Connected");
                    pingTimer.Stop();
                    pingTimer.Start();
                    break;
                }
                catch (Exception e)
                {
                    Log.Error(e, "Connection error");
                    OnError?.Invoke(inst, "Connection error, retrying...");
                }

                await Task.Delay(5000);
            }
            return true;
        }

        public void Disconnect()
        {
            cts.Cancel();
            ws.Abort();
            ws.Dispose();
        }

        public async void Login(string username, string password)
        {
            await Connect();

            await Send(ws, "40");
            await Send(ws, $"42[\"login\",{{\"username\":\"{username}\",\"password\":\"{password}\"}}]");
            ProcessMessages();
        }

        public async void Resume(string jwt)
        {
            await Connect();

            await Send(ws, "40");
            await Send(ws, $"42[\"resume\",\"{jwt}\"]");
            ProcessMessages();
        }

        private async void ProcessMessages()
        {
            while (true)
            {
                var tok = cts.Token;
                try
                {
                    var resp = await Receive(ws);
                    if (resp == "2") // ping
                    {
                        pingTimer.Stop();
                        pingTimer.Start();
                        await Send(ws, "3"); // pong
                    }
                    else if (resp.Contains("loggedIn"))
                    {
                        OnLoggedIn?.Invoke(inst, null);
                        var msg = resp.Substring(2, resp.Length - 2);
                        dynamic json = JArray.Parse(msg);
                        var jwt = json[1];
                        Properties.Settings.Default.JWT = jwt;
                        Properties.Settings.Default.Save();
                        this.jwt = jwt;
                    }
                    else if (resp.Contains("LOGIN_FAILED"))
                    {
                        OnLoginFailed?.Invoke(inst, null);
                    }
                    else if (resp.Contains("balance"))
                    {
                        var msg = resp.Substring(2, resp.Length - 2);
                        dynamic json = JArray.Parse(msg);
                        var balance = json[1];
                        OnBalance?.Invoke(inst, balance.Value);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error receiving data");
                    break;
                }
                if (tok.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
