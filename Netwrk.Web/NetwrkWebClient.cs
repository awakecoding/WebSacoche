using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Netwrk.Web
{
    internal class NetwrkWebClient
    {
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;

        public Stream Stream { get; private set; }

        public NetwrkWebClient(TcpClient client)
        {
            this.client = client;

            SetStream(client.GetStream());
        }

        public async Task SslAuthenticateAsServerAsync(X509Certificate2 certificate)
        {
            SslStream sslStream = new SslStream(Stream);
            await sslStream.AuthenticateAsServerAsync(certificate).ConfigureAwait(false);
            SetStream(sslStream);
        }

        public async Task SslAuthenticateAsClientAsync(string targetHost)
        {
            SslStream sslStream = new SslStream(Stream);
            await sslStream.AuthenticateAsClientAsync(targetHost).ConfigureAwait(false);
            SetStream(sslStream);
        }

        public async Task<T> ReceiveAsync<T>() where T : NetwrkWebMessage, new()
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

            T message = new T();
            
            message.Parse(lines.ToArray());

            string lengthValue = message.Headers[NetwrkKnownHttpHeaders.ContentLength];

            if (lengthValue != null)
            {
                if (!int.TryParse(lengthValue, out var length))
                {
                    return null;
                }

                byte[] data = new byte[length];

                await client.GetStream().ReadAsync(data, 0, length);

                message.Data = data;
            }

            return message;
        }

        public async Task SendAsync(NetwrkWebMessage message)
        {
            var mess = message.ToString();
            mess = mess.Replace("\n", "\r\n");
            mess = string.Concat(mess, "\r\n");
            
            await writer.WriteLineAsync(mess);

            if (message.Data != null && message.Data.Length > 0)
            {
                Stream.Write(message.Data, 0, message.Data.Length);
                Stream.Flush();
            }
        }

        private void SetStream(Stream stream)
        {
            this.Stream = stream;
            writer = new StreamWriter(stream);
            reader = new StreamReader(stream);

            writer.AutoFlush = true;
        }
    }
}
