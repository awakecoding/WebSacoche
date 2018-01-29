﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
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
