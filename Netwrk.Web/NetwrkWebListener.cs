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

namespace Netwrk.Web
{
    public class NetwrkWebListener
    {
        public delegate NetwrkWebResponse RequestEventHandler(NetwrkWebListener listener, NetwrkWebRequest request);
        public delegate bool WebSocketRequestEventHandler(NetwrkWebListener listener, NetwrkWebRequest request);
        public delegate void WebSocketConnectionEventHandler(NetwrkWebListener listener, NetwrkWebSocket webSocket);

        private TcpListener listener;
        private CancellationTokenSource cancellationTokenSource;

        public IPAddress LocalAddress { get; }
        public X509Certificate2 Certificate { get; }
        public int Port { get; private set; }

        public bool Listening { get; private set; }

        public event RequestEventHandler OnRequest;
        public event WebSocketRequestEventHandler OnWebsocketRequest;
        public event WebSocketConnectionEventHandler OnWebSocketConnection;

        public NetwrkWebListener(int port, IPAddress localAddress = null, X509Certificate2 certificate = null)
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
            NetwrkWebClient webClient = new NetwrkWebClient(client);

            if (Certificate != null)
            {
                await webClient.SslAuthenticateAsServerAsync(Certificate);
            }

            NetwrkWebRequest request = await webClient.ReceiveAsync<NetwrkWebRequest>();
            NetwrkWebSocket webSocket = new NetwrkWebSocket(client, webClient.Stream);

            if (request == null)
            {
                return false;
            }

            if (request.IsWebSocketRequest)
            {
                if (OnWebsocketRequest?.Invoke(this, request) ?? true)
                {
                    NetwrkWebResponse response = new NetwrkWebResponse
                    {
                        Version = request.Version,
                        StatusCode = NetwrkHttpStatusCode.SwitchingProtocols
                    };

                    response.Headers[NetwrkKnownHttpHeaders.Upgrade] = "websocket";
                    response.Headers[NetwrkKnownHttpHeaders.Connection] = "Upgrade";
                    response.Headers[NetwrkKnownHttpHeaders.Server] = "WebSacoche";

                    using (var sha1 = SHA1.Create())
                    {
                        string clientKey = request.Headers[NetwrkKnownHttpHeaders.SecWebSocketKey];
                        string accept = clientKey + NetwrkWebSocket.WS_MAGIC_GUID;
                        byte[] acceptBytes = Encoding.UTF8.GetBytes(accept);
                        byte[] acceptSha1 = sha1.ComputeHash(acceptBytes);
                        string serverKey = Convert.ToBase64String(acceptSha1);
                        response.Headers[NetwrkKnownHttpHeaders.SecWebSocketAccept] = serverKey;
                    }

                    await webClient.SendAsync(response);

                    OnWebSocketConnection?.Invoke(this, webSocket);

                    webSocket.Start();
                }
            }
            else
            {
                NetwrkWebResponse response = OnRequest?.Invoke(this, request) ?? new NetwrkWebResponse
                {
                    Version = request.Version,
                    StatusCode = NetwrkHttpStatusCode.InternalServerError
                };

                await webClient.SendAsync(response);

                client.Close();
            }

            return true;
        }
    }
}
