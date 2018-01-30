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
                var authenticated = await webClient.SslAuthenticateAsServerAsync(Certificate);

                if (!authenticated)
                    return false;
            }

            SacocheWebSocket webSocket = new SacocheWebSocket(client, webClient.Stream);

            string[] lines = await webClient.ReceiveAsync();

            if (lines == null)
                return false;

            SacocheHttp.ParseRequestLine(lines[0], out var method, out var path, out var version);

            if (method != "GET")
                return false;

            if (SacocheHttp.GetFieldValue(lines, "Connection") != "Upgrade")
                return false;

            if (SacocheHttp.GetFieldValue(lines, "Upgrade") != "websocket")
                return false;

            if (SacocheHttp.GetFieldValue(lines, "Sec-WebSocket-Version") != "13")
                return false;

            if (SacocheHttp.GetFieldValue(lines, "Sec-WebSocket-Key").Length < 1)
                return false;

            string clientKey = SacocheHttp.GetFieldValue(lines, "Sec-WebSocket-Key");
            string serverKey = SacocheWebSocket.ComputeServerKey(clientKey);

            StringBuilder sb = new StringBuilder();
            sb.Append("HTTP/1.1 101 Switching Protocols\r\n");
            sb.Append("Upgrade: websocket\r\n");
            sb.Append("Connection: Upgrade\r\n");
            sb.Append("Server: WebSacoche\r\n");
            sb.Append("Sec-WebSocket-Accept: " + serverKey + "\r\n");
            string message = sb.ToString();

            await webClient.SendAsync(message);

            OnWebSocketConnection?.Invoke(this, webSocket);

            webSocket.Start();

            return true;
        }
    }
}
