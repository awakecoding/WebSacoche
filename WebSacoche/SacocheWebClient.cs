using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Sacoche
{
    internal class SacocheWebClient
    {
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;

        public Stream Stream { get; private set; }

        public SacocheWebClient(TcpClient client)
        {
            this.client = client;
            SetStream(client.GetStream());
        }

        public async Task<bool> SslAuthenticateAsServerAsync(X509Certificate2 certificate)
        {
            var sslStream = new SslStream(Stream);
            const SslProtocols proto = SslProtocols.Tls12 | SslProtocols.Tls11;

            try
            {
                await sslStream.AuthenticateAsServerAsync(certificate, false, proto, false);
                SetStream(sslStream);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to authenticate client : {e}");
                
                return false;
            }
        }

        public async Task<bool> SslAuthenticateAsClientAsync(string targetHost)
        {
            var sslStream = new SslStream(Stream);
            
            try
            {
                await sslStream.AuthenticateAsClientAsync(targetHost).ConfigureAwait(false);
                SetStream(sslStream);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unable to authenticate targeted host {targetHost} : {e}");
                
                return false;
            }
        }
        
        public async Task<string[]> ReceiveAsync()
        {
            List<string> lines = new List<string>();

            while (true)
            {
                string line;
                line = await reader.ReadLineAsync();

                if (line == null)
                {
                    return null;
                }

                if (string.IsNullOrEmpty(line))
                {
                    break;
                }

                lines.Add(line);
            }

            return lines.ToArray();
        }

        public async Task SendAsync(string message)
        {          
            await writer.WriteLineAsync(message);
        }

        private void SetStream(Stream stream)
        {
            this.Stream = stream;
            writer = new StreamWriter(stream);
            reader = new StreamReader(stream);
            writer.AutoFlush = true;
            writer.NewLine = "\r\n";
        }
    }
}
