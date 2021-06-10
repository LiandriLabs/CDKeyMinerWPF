using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CDKeyMiner
{
    static class LoginHelper
    {
        static string URL = "wss://app.cdkeyminer.com/socket.io/?EIO=4&transport=websocket";

        static async Task Send(ClientWebSocket socket, string data)
        {
            await socket.SendAsync(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes(data)),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        static async Task<string> Receive(ClientWebSocket socket)
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);
            WebSocketReceiveResult result;
            using (var ms = new MemoryStream())
            {
                do
                {
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                } while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(ms, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public static async Task<bool> LogIn(string username, string password)
        {
            using (var socket = new ClientWebSocket())
            {
                await socket.ConnectAsync(new Uri(URL), CancellationToken.None);

                await Send(socket, "40");
                await Send(socket, $"42[\"login\",{{\"username\":\"{username}\",\"password\":\"{password}\"}}]");

                await Receive(socket); // handshake
                await Receive(socket); // handshake
                var resp = await Receive(socket);
                return resp.Contains("loggedIn");
            }
        }
    }
}
