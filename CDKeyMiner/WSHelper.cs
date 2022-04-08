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
using System.Globalization;

namespace CDKeyMiner
{
    public class WSHelper
    {
        private static WSHelper inst = null;

#if DEBUG
        private const string URL = "ws://localhost:81/socket.io/?EIO=4&transport=websocket";
#else
        private const string URL = "wss://app.cdkeyminer.com/socket.io/?EIO=4&transport=websocket";
#endif

        private ClientWebSocket ws;
        private SemaphoreSlim sendMutex = new SemaphoreSlim(1);
        private SemaphoreSlim recvMutex = new SemaphoreSlim(1);
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

        private async Task Send(string data)
        {
            Log.Debug("WS Send: {Data}", data);
            await sendMutex.WaitAsync().ConfigureAwait(false);

            try
            {
                await ws.SendAsync(
                    new ArraySegment<byte>(Encoding.UTF8.GetBytes(data)),
                    WebSocketMessageType.Text,
                    true,
                    cts.Token);
            }
            finally
            {
                sendMutex.Release();
            }
        }

        private async Task<string> Receive()
        {
            Log.Debug("WS Receive");
            await recvMutex.WaitAsync().ConfigureAwait(false);

            try
            {
                var buffer = new ArraySegment<byte>(new byte[2048]);
                WebSocketReceiveResult result;
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await ws.ReceiveAsync(buffer, cts.Token);
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
            finally
            {
                recvMutex.Release();
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
            Log.Information("Disconnecting WebSockets");
            cts.Cancel();
            ws.Abort();
            ws.Dispose();
        }

        public async void Login(string username, string password)
        {
            await Connect();

            await Send("40");
            await Receive();
            await Receive();
            await Send($"42[\"login\",{{\"username\":\"{username}\",\"password\":\"{password}\"}}]");
            ProcessMessages();
        }

        public async void Resume(string jwt)
        {
            await Connect();

            await Send("40");
            await Receive();
            await Receive();
            await Send($"42[\"resume\",\"{jwt}\"]");
            ProcessMessages();
        }

        public async void ReportHashrate(double hr)
        {
            try
            {
                await Send($"42[\"hashrate\",{hr.ToString("N2", CultureInfo.InvariantCulture)}]");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Report hash rate error");
            }
        }

        public async void ReportTemperature(int temp)
        {
            try
            {
                await Send($"42[\"temperature\", {temp}]");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Report temperature error");
            }
        }

        private async void ProcessMessages()
        {
            while (true)
            {
                var tok = cts.Token;
                try
                {
                    var resp = await Receive();
                    if (resp == "2") // ping
                    {
                        pingTimer.Stop();
                        pingTimer.Start();
                        await Send("3"); // pong
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
                    else if (resp.Contains("LOGIN_FAILED") || resp.Contains("INVALID_SESSION"))
                    {
                        this.Disconnect();
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
