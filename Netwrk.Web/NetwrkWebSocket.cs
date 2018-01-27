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
        public const string WS_MAGIC_GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private static readonly object randomLock = new object();
        private static readonly Random random = new Random();

        public delegate void TextMessageEventHander(NetwrkWebSocket socket, string message);
        public delegate void BinaryMessageEventHander(NetwrkWebSocket socket, byte[] data);
        public delegate void CloseEventHander(NetwrkWebSocket socket);

        private TcpClient client;
        private Stream stream;
        private byte[] smallBuffer = new byte[256];
        private int maskingKey;
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
            if (client)
            {
                lock (randomLock)
                {
                    maskingKey = random.Next(1, int.MaxValue);
                }
            }
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

                string webSocketKey = CreateSecWebSocketKey();

                request.Headers[NetwrkKnownHttpHeaders.Connection] = "Upgrade";
                request.Headers[NetwrkKnownHttpHeaders.Upgrade] = "websocket";
                request.Headers[NetwrkKnownHttpHeaders.Host] = uri.Host;
                request.Headers[NetwrkKnownHttpHeaders.SecWebSocketVersion] = "13";
                request.Headers[NetwrkKnownHttpHeaders.SecWebSocketKey] = webSocketKey;

                await webClient.SendAsync(request);

                NetwrkWebResponse response = await webClient.ReceiveAsync<NetwrkWebResponse>();

                if (!response.IsWebSocketAccepted || !response.IsKeyValid(webSocketKey))
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
                Masked = maskingKey != 0,
                MaskingKey = maskingKey,
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
                Masked = maskingKey != 0,
                MaskingKey = maskingKey,
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
                            Masked = maskingKey != 0,
                            MaskingKey = maskingKey,
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

            byte flags = await ReadByteAsync();
            packet.Fin = (flags & 0b1000_0000) != 0;
            packet.OpCode = (OpCode)(flags & 0b0000_1111);

            byte lengthByte = await ReadByteAsync();
            packet.Masked = (lengthByte & 0b1000_0000) != 0;
            lengthByte &= 0b0111_1111;

            long length = lengthByte;

            if (lengthByte == 127)
            {
                length = IPAddress.NetworkToHostOrder(await ReadLongAsync());
            }
            else if (lengthByte == 126)
            {
                length = IPAddress.NetworkToHostOrder(await ReadShortAsync());
            }

            packet.PayloadData = new byte[length];

            if (packet.Masked)
            {
                await ReadBytesAsync(mask, 4);
            }

            await ReadBytesAsync(packet.PayloadData, (int)length);

            if (packet.Masked)
            {
                Unmask(packet.PayloadData, mask);
            }

            return packet;
        }

        private async Task<byte> ReadByteAsync()
        {
            await ReadBytesAsync(smallBuffer, sizeof(byte));
            return smallBuffer[0];
        }

        private async Task<short> ReadShortAsync()
        {
            await ReadBytesAsync(smallBuffer, sizeof(short));
            return BitConverter.ToInt16(smallBuffer, 0);
        }

        private async Task<int> ReadIntAsync()
        {
            await ReadBytesAsync(smallBuffer, sizeof(int));
            return BitConverter.ToInt32(smallBuffer, 0);
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

        //TODO: Implement fragmentation (add async for this)
        private void Send(WebSocketPacket packet)
        {
            byte[] crlf = new byte[4];
            crlf[0] = 0x0D;
            crlf[1] = 0x0A;
            crlf[2] = 0x0D;
            crlf[3] = 0x0A;            
            
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(memoryStream);

                writer.Write((byte)(0b1000_0000 | (byte)packet.OpCode));
                
                writer.Write(crlf);

                byte masked = (byte)(packet.Masked ? 0b1000_0000 : 0);

                if (packet.PayloadLength > short.MaxValue)
                {
                    writer.Write((byte)(masked | 127));
                    writer.Write(crlf);
                    writer.Write(IPAddress.HostToNetworkOrder((long)packet.PayloadLength));
                    writer.Write(crlf);
                }
                else if (packet.PayloadLength > 125)
                {
                    writer.Write((byte)(masked | 126));
                    writer.Write(crlf);
                    writer.Write(IPAddress.HostToNetworkOrder((short)packet.PayloadLength));
                    writer.Write(crlf);
                }
                else
                {
                    writer.Write((byte)(masked | (byte)packet.PayloadLength));
                    writer.Write(crlf);
                }

                if (packet.PayloadLength > 0)
                {
                    byte[] data = packet.PayloadData;

                    if (packet.MaskingKey != 0)
                    {
                        writer.Write(IPAddress.HostToNetworkOrder(packet.MaskingKey));
                        writer.Write(crlf);

                        data = Mask(packet.PayloadData, packet.MaskingKey);
                    }

                    memoryStream.Write(data, 0, data.Length);
                    writer.Write(crlf);
                }

                stream.Write(memoryStream.ToArray(), 0, (int)memoryStream.Length);
                stream.Flush();
            }
        }

        private static string CreateSecWebSocketKey()
        {
            byte[] data = new byte[16];

            random.NextBytes(data);

            return Convert.ToBase64String(data);
        }

        private static byte[] Mask(byte[] data, int maskingKey)
        {
            byte[] result = new byte[data.Length];
            byte[] maskingKeyBytes = BitConverter.GetBytes(maskingKey);

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (byte)(data[i] ^ maskingKeyBytes[i % 4]);
            }

            return result;
        }

        private static void Unmask(byte[] data, byte[] mask)
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

            public int MaskingKey { get; set; }

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