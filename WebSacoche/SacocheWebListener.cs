using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sacoche
{
    public class SacocheWebListener
    {
        public delegate void WebSocketConnectionEventHandler(SacocheWebListener listener, SacocheWebSocket webSocket);

        private TcpListener listener;
        private CancellationTokenSource cancellationTokenSource;

        public IPAddress LocalAddress { get; }
        public X509Certificate2 Certificate { get; }
        public int Port { get; private set; }

        public bool Listening { get; private set; }

        public event WebSocketConnectionEventHandler OnWebSocketConnection;

        public SacocheWebListener(int port, IPAddress localAddress = null, X509Certificate2 certificate = null)
        {
            Port = port;
            LocalAddress = localAddress ?? IPAddress.Any;
            Certificate = certificate;

            cancellationTokenSource = new CancellationTokenSource();
        }

        public bool Start()
        {
            if (listener != null)
            {
                return true;
            }

            listener = new TcpListener(LocalAddress, Port);

            try
            {
                listener.Start();
            }
            catch
            {
                Stop();
            }

            Port = (listener.LocalEndpoint as IPEndPoint).Port;
            Listening = true;

            AcceptClients();

            return true;
        }

        public void Stop()
        {
            if (listener == null)
            {
                return;
            }
            
            cancellationTokenSource.Cancel();

            Listening = false;
            listener.Stop();
            listener = null;
        }

        private async void AcceptClients()
        {
            while (!cancellationTokenSource.IsCancellationRequested && Listening)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();

                client.NoDelay = true;

                var t = Task.Run(async delegate
                {
                    if (!await HandleClient(client))
                    {
                        client.Close();
                    }
                }, cancellationTokenSource.Token);
            }
        }

        private async Task<bool> HandleClient(TcpClient client)
        {
            SacocheWebClient webClient = new SacocheWebClient(client);

            if (Certificate != null)
            {
                await webClient.SslAuthenticateAsServerAsync(Certificate);
            }

            SacocheWebRequest request = await webClient.ReceiveAsync<SacocheWebRequest>();
            SacocheWebSocket webSocket = new SacocheWebSocket(client, webClient.Stream);

            if (request == null)
            {
                return false;
            }

            if (request.Method != "GET")
                return false;

            if (request.Headers["Connection"] != "Upgrade")
                return false;

            if (request.Headers["Upgrade"] != "websocket")
                return false;

            if (request.Headers["Sec-WebSocket-Version"] != "13")
                return false;

            if (!request.Headers.HasValue("Sec-WebSocket-Key"))
                return false;

            SacocheWebResponse response = new SacocheWebResponse
            {
                Version = request.Version,
                Code = 101,
                Reason = "Switching Protocols"
            };

            response.Headers["Upgrade"] = "websocket";
            response.Headers["Connection"] = "Upgrade";
            response.Headers["Server"] = "WebSacoche";

            string clientKey = request.Headers["Sec-WebSocket-Key"];
            string serverKey = SacocheWebSocket.ComputeServerKey(clientKey);
            response.Headers["Sec-WebSocket-Accept"] = serverKey;

            await webClient.SendAsync(response);

            OnWebSocketConnection?.Invoke(this, webSocket);

            webSocket.Start();

            return true;
        }
    }
}
