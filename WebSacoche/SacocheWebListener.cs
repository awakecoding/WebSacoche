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
            LoadCertStore();
            cancellationTokenSource = new CancellationTokenSource();
        }

        private bool LoadCertStore()
        {
            /* https://github.com/dotnet/corefx/issues/26061 */

            X509Certificate2 cert = Certificate;

            if (cert == null)
                return false;

            try
            {
                var chain = new X509Chain
                {
                    ChainPolicy =
                    {
                        VerificationFlags = X509VerificationFlags.AllFlags,
                        RevocationFlag = X509RevocationFlag.ExcludeRoot,
                        RevocationMode = X509RevocationMode.NoCheck
                    }
                };
                
                chain.Build(cert);

                using (var userIntermediateStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser))
                {
                    userIntermediateStore.Open(OpenFlags.ReadWrite);

                    foreach (var element in chain.ChainElements)
                    {
                        if (element.Certificate.Thumbprint == cert.Thumbprint)
                            continue;

                        var found = userIntermediateStore.Certificates
                            .Find(X509FindType.FindBySerialNumber, element.Certificate.SerialNumber, false);

                        if (found.Count != 0)
                            continue;

                        userIntermediateStore.Add(element.Certificate);
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
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

                HandleClientAsync(client);
            }
        }

        private async void HandleClientAsync(TcpClient client)
        {
            if (!await HandleClient(client))
            {
                client.Close();
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

            bool success = await webSocket.AcceptAsync(webClient);

            if (!success)
                return false;

            OnWebSocketConnection?.Invoke(this, webSocket);

            webSocket.Start();

            return true;
        }
    }
}
