using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Netwrk.Web
{
    public class NetwrkWebSocket
    {
        public const byte WS_BIT_FIN = 0x80;
        public const byte WS_BIT_MASK = 0x80;
        public const byte WS_OPCODE_MASK = 0x0F;
        public const byte WS_PAYLOAD_MASK = 0x7F;
        public const byte WS_PAYLOAD_16 = 126;
        public const byte WS_PAYLOAD_64 = 127;
        public const string WS_MAGIC_GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private static readonly Random random = new Random();

        public delegate void TextMessageEventHander(NetwrkWebSocket socket, string message);
        public delegate void BinaryMessageEventHander(NetwrkWebSocket socket, byte[] data);
        public delegate void CloseEventHander(NetwrkWebSocket socket);

        private TcpClient client;
        private Stream stream;
        private byte[] smallBuffer = new byte[256];

        private Task receivingTask;

        public event TextMessageEventHander OnTextMessage;
        public event BinaryMessageEventHander OnBinaryMessage;
        public event CloseEventHander OnClose;

        public bool Connected { get; private set; }

        internal NetwrkWebSocket(TcpClient client, Stream stream)
        {
            InitializeConnected(client, stream);
        }

        private void InitializeConnected(TcpClient client, Stream stream)
        {
            this.client = client;
            this.stream = stream;

            Connected = true;
        }

        public NetwrkWebSocket(bool client = true)
        {

        }

        public async Task<bool> ConnectAsync(Uri uri, bool secure = false)
        {
            if (Connected)
            {
                return true;
            }

            try
            {
                TcpClient client = new TcpClient();
                client.NoDelay = true;

                await client.ConnectAsync(uri.Host, uri.Port).ConfigureAwait(false);

                NetwrkWebClient webClient = new NetwrkWebClient(client);

                if (secure)
                {
                    await webClient.SslAuthenticateAsClientAsync(uri.Host);
                }

                NetwrkWebRequest request = new NetwrkWebRequest
                {
                    Path = uri.AbsolutePath
                };

                string clientKey = GenerateClientKey();

                request.Headers[NetwrkKnownHttpHeaders.Connection] = "Upgrade";
                request.Headers[NetwrkKnownHttpHeaders.Upgrade] = "websocket";
                request.Headers[NetwrkKnownHttpHeaders.Host] = uri.Host;
                request.Headers[NetwrkKnownHttpHeaders.SecWebSocketVersion] = "13";
                request.Headers[NetwrkKnownHttpHeaders.SecWebSocketKey] = clientKey;

                await webClient.SendAsync(request);

                NetwrkWebResponse response = await webClient.ReceiveAsync<NetwrkWebResponse>();

                if (!response.IsWebSocketAccepted || !response.IsKeyValid(clientKey))
                {
                    return false;
                }

                InitializeConnected(client, webClient.Stream);
                Start();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Start()
        {
            if (receivingTask == null && Connected)
            {
                receivingTask = ReceiveAsync();
            }
        }

        public void Stop()
        {
            if (client == null)
            {
                return;
            }

            Connected = false;

            OnClose?.Invoke(this);

            client.Close();
            client = null;
            stream = null;
        }

        public void Send(string message)
        {
            WebSocketPacket packet = new WebSocketPacket
            {
                Fin = true,
                OpCode = OpCode.Text,
                PayloadData = Encoding.UTF8.GetBytes(message)
            };

            Send(packet);
        }

        public void Send(byte[] data)
        {
            WebSocketPacket packet = new WebSocketPacket
            {
                Fin = true,
                OpCode = OpCode.Binary,
                PayloadData = data
            };

            Send(packet);
        }

        private async Task ReceiveAsync()
        {
            MemoryStream memoryStream = new MemoryStream();
            OpCode lastOpCode = OpCode.Close;

            while (true)
            {
                WebSocketPacket packet;

                try
                {
                    packet = await ReadPacketHeaderAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Stop();
                    break;
                }

                OpCode opCode = packet.OpCode == OpCode.Continuation ? lastOpCode : packet.OpCode;

                switch (opCode)
                {
                    case OpCode.Text:
                    case OpCode.Binary:
                        memoryStream.Write(packet.PayloadData, 0, packet.PayloadData.Length);

                        if (packet.Fin)
                        {
                            HandleMessage(opCode, memoryStream.ToArray());
                            memoryStream.SetLength(0);
                        }
                        break;
                    case OpCode.Ping:
                        Send(new WebSocketPacket
                        {
                            Fin = true,
                            OpCode = OpCode.Pong
                        });
                        break;
                    case OpCode.Pong:
                        break;
                    default:
                        Stop();
                        break;
                }

                lastOpCode = opCode;
            }
        }

        private void HandleMessage(OpCode opCode, byte[] data)
        {
            if (opCode == OpCode.Text)
            {
                OnTextMessage?.Invoke(this, Encoding.UTF8.GetString(data));
            }
            else
            {
                OnBinaryMessage?.Invoke(this, data);
            }
        }

        private async Task<WebSocketPacket> ReadPacketHeaderAsync()
        {
            byte[] mask = new byte[4];
            WebSocketPacket packet = new WebSocketPacket();

            byte[] hdrData = new byte[2];
            await ReadBytesAsync(hdrData, 2);

            packet.Fin = (hdrData[0] & WS_BIT_FIN) != 0;
            packet.OpCode = (OpCode)(hdrData[0] & WS_OPCODE_MASK);

            packet.Masked = (hdrData[1] & WS_BIT_MASK) != 0;
            long payloadSize = (hdrData[1] & WS_PAYLOAD_MASK);

            if (payloadSize == WS_PAYLOAD_16)
            {
                payloadSize = IPAddress.NetworkToHostOrder(await ReadShortAsync());
            }
            else if (payloadSize == WS_PAYLOAD_64)
            {
                payloadSize = IPAddress.NetworkToHostOrder(await ReadLongAsync());
            }

            packet.PayloadData = new byte[payloadSize];

            if (packet.Masked)
            {
                await ReadBytesAsync(mask, 4);
            }

            await ReadBytesAsync(packet.PayloadData, (int) payloadSize);

            if (packet.Masked)
            {
                Mask(packet.PayloadData, mask);
            }

            return packet;
        }

        private async Task<short> ReadShortAsync()
        {
            await ReadBytesAsync(smallBuffer, sizeof(short));
            return BitConverter.ToInt16(smallBuffer, 0);
        }

        private async Task<long> ReadLongAsync()
        {
            await ReadBytesAsync(smallBuffer, sizeof(long));
            return BitConverter.ToInt64(smallBuffer, 0);
        }

        private async Task<int> ReadBytesAsync(byte[] buffer, int count)
        {
            return await stream.ReadAsync(buffer, 0, count);
        }

        private void Send(WebSocketPacket packet)
        {            
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(memoryStream);

                byte[] mask = new byte[4];
                random.NextBytes(mask);

                writer.Write((byte)(WS_BIT_FIN | (byte)packet.OpCode));

                byte masked = (byte)(packet.Masked ? WS_BIT_MASK : 0);

                if (packet.PayloadLength < 126)
                {
                    writer.Write((byte)(masked | (byte)packet.PayloadLength));
                }
                else if (packet.PayloadLength <= 0xFFFF)
                {
                    writer.Write((byte)(masked | WS_PAYLOAD_16));
                    writer.Write(IPAddress.HostToNetworkOrder((short)packet.PayloadLength));
                }
                else
                {
                    writer.Write((byte)(masked | WS_PAYLOAD_64));
                    writer.Write(IPAddress.HostToNetworkOrder((long)packet.PayloadLength));
                }

                if (packet.PayloadLength > 0)
                {
                    if (packet.Masked)
                    {
                        writer.Write(mask);
                        Mask(packet.PayloadData, mask);
                    }

                    memoryStream.Write(packet.PayloadData, 0, packet.PayloadData.Length);
                }

                stream.Write(memoryStream.ToArray(), 0, (int) memoryStream.Length);
                stream.Flush();
            }
        }

        private static string GenerateClientKey()
        {
            byte[] data = new byte[16];
            random.NextBytes(data);
            return Convert.ToBase64String(data);
        }

        private static void Mask(byte[] data, byte[] mask)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= mask[i % 4];
            }
        }

        private class WebSocketPacket
        {
            public OpCode OpCode { get; set; }

            public bool Fin { get; set; }

            public bool Masked { get; set; }

            public byte[] PayloadData { get; set; }

            public int PayloadLength => PayloadData?.Length ?? 0;
        }

        private enum OpCode
        {
            Continuation = 0,
            Text = 1,
            Binary = 2,
            Close = 8,
            Ping = 9,
            Pong = 10
        }
    }
}