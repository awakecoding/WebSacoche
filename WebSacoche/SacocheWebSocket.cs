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

/**
 * The WebSocket Protocol:
 * https://tools.ietf.org/html/rfc6455
 *
 * WebSocket API:
 * https://msdn.microsoft.com/en-us/library/hh772770/
 *
 * Web sockets:
 * https://html.spec.whatwg.org/multipage/comms.html#network
 *
 * MessageEvent:
 * https://html.spec.whatwg.org/multipage/comms.html#messageevent
 *
 * CloseEvent:
 * https://html.spec.whatwg.org/multipage/comms.html#the-closeevent-interfaces
 *
 * The WebSocket Server API:
 * http://w3c-jseverywhere.github.io/websocket-server/
 *
 * Writing WebSocket servers:
 * https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_servers
 *
 */

/**
 * 5.2 Base Framing Protocol
 * https://tools.ietf.org/html/rfc6455#section-5.2
 *
 *  0                   1                   2                   3
 *  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
 * +-+-+-+-+-------+-+-------------+-------------------------------+
 * |F|R|R|R| opcode|M| Payload len |    Extended payload length    |
 * |I|S|S|S|  (4)  |A|     (7)     |             (16/64)           |
 * |N|V|V|V|       |S|             |   (if payload len==126/127)   |
 * | |1|2|3|       |K|             |                               |
 * +-+-+-+-+-------+-+-------------+ - - - - - - - - - - - - - - - +
 * |     Extended payload length continued, if payload len == 127  |
 * + - - - - - - - - - - - - - - - +-------------------------------+
 * |                               |Masking-key, if MASK set to 1  |
 * +-------------------------------+-------------------------------+
 * | Masking-key (continued)       |          Payload Data         |
 * +-------------------------------- - - - - - - - - - - - - - - - +
 * :                     Payload Data continued ...                :
 * + - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - +
 * |                     Payload Data continued ...                |
 * +---------------------------------------------------------------+
 */

namespace Sacoche
{
    public class SacocheWebSocket
    {
        public const byte WS_BIT_FIN = 0x80;
        public const byte WS_BIT_MASK = 0x80;
        public const byte WS_OPCODE_MASK = 0x0F;
        public const byte WS_OPCODE_CONTINUATION = 0;
        public const byte WS_OPCODE_TEXT = 1;
        public const byte WS_OPCODE_BINARY = 2;
        public const byte WS_OPCODE_CLOSE = 8;
        public const byte WS_OPCODE_PING = 9;
        public const byte WS_OPCODE_PONG = 10;
        public const byte WS_PAYLOAD_MASK = 0x7F;
        public const byte WS_PAYLOAD_16 = 126;
        public const byte WS_PAYLOAD_64 = 127;
        public const string WS_MAGIC_GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        public const ushort WS_READY_STATE_CONNECTING = 0;
        public const ushort WS_READY_STATE_OPEN = 1;
        public const ushort WS_READY_STATE_CLOSING = 2;
        public const ushort WS_READY_STATE_CLOSED = 3;

        public const int WS_CLOSE_STATUS_NORMAL = 1000;
        public const int WS_CLOSE_STATUS_GOING_AWAY = 1001;
        public const int WS_CLOSE_STATUS_PROTOCOL_ERROR = 1002;
        public const int WS_CLOSE_STATUS_UNKNOWN_DATA = 1003;
        public const int WS_CLOSE_STATUS_INCONSISTENT_DATA = 1007;
        public const int WS_CLOSE_STATUS_POLICY_VIOLATION = 1008;
        public const int WS_CLOSE_STATUS_MESSAGE_TOO_BIG = 1009;
        public const int WS_CLOSE_STATUS_EXTENSION_EXPECTED = 1010;
        public const int WS_CLOSE_STATUS_SERVER_ERROR = 1011;

        private static readonly Random random = new Random();

        public delegate void TextMessageEventHander(SacocheWebSocket socket, string message);
        public delegate void BinaryMessageEventHander(SacocheWebSocket socket, byte[] data);
        public delegate void CloseEventHander(SacocheWebSocket socket, bool clean, ushort code, string reason);

        bool server;
        private TcpClient client;
        private Stream stream;

        private Task receivingTask;

        public event TextMessageEventHander OnTextMessage;
        public event BinaryMessageEventHander OnBinaryMessage;

        public event CloseEventHander OnClose;

        public int ReadyState { get; private set; }

        public SacocheWebSocket()
        {
            this.server = false;
        }

        internal SacocheWebSocket(TcpClient client, Stream stream)
        {
            this.server = true;
            InitializeConnected(client, stream);
        }

        private void InitializeConnected(TcpClient client, Stream stream)
        {
            this.client = client;
            this.stream = stream;
            ReadyState = WS_READY_STATE_OPEN;
        }

        public async Task<bool> ConnectAsync(Uri uri, bool secure = false)
        {
            if (ReadyState == WS_READY_STATE_OPEN)
            {
                return true;
            }

            try
            {
                TcpClient client = new TcpClient();
                client.NoDelay = true;

                await client.ConnectAsync(uri.Host, uri.Port).ConfigureAwait(false);

                SacocheWebClient webClient = new SacocheWebClient(client);

                if (secure)
                {
                    await webClient.SslAuthenticateAsClientAsync(uri.Host);
                }

                SacocheWebRequest request = new SacocheWebRequest
                {
                    Path = uri.AbsolutePath
                };

                string clientKey = GenerateClientKey();

                request.Headers["Connection"] = "Upgrade";
                request.Headers["Upgrade"] = "websocket";
                request.Headers["Host"] = uri.Host;
                request.Headers["Sec-WebSocket-Version"] = "13";
                request.Headers["Sec-WebSocket-Key"] = clientKey;

                await webClient.SendAsync(request);

                SacocheWebResponse response = await webClient.ReceiveAsync<SacocheWebResponse>();

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
            if ((receivingTask == null) && (ReadyState == WS_READY_STATE_OPEN))
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

            ReadyState = WS_READY_STATE_CLOSED;

            client.Close();
            client = null;
            stream = null;
        }

        public void Send(string message)
        {
            WebSocketPacket packet = new WebSocketPacket
            {
                Fin = true,
                Opcode = WS_OPCODE_TEXT,
                PayloadData = Encoding.UTF8.GetBytes(message)
            };

            Send(packet);
        }

        public void Send(byte[] data)
        {
            WebSocketPacket packet = new WebSocketPacket
            {
                Fin = true,
                Opcode = WS_OPCODE_BINARY,
                PayloadData = data
            };

            Send(packet);
        }

        private async Task ReceiveAsync()
        {
            MemoryStream memoryStream = new MemoryStream();
            byte lastOpCode = WS_OPCODE_CLOSE;

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

                byte opcode = packet.Opcode == WS_OPCODE_CONTINUATION ? lastOpCode : packet.Opcode;

                if ((opcode == WS_OPCODE_TEXT) || (opcode == WS_OPCODE_BINARY))
                {
                        memoryStream.Write(packet.PayloadData, 0, packet.PayloadData.Length);

                        if (packet.Fin)
                        {
                            byte[] data = memoryStream.ToArray();

                            if (opcode == WS_OPCODE_TEXT)
                            {
                                OnTextMessage?.Invoke(this, Encoding.UTF8.GetString(data));
                            }
                            else
                            {
                                OnBinaryMessage?.Invoke(this, data);
                            }

                            memoryStream.SetLength(0);
                        }
                }
                else if (opcode == WS_OPCODE_PING)
                {
                        Send(new WebSocketPacket
                        {
                            Fin = true,
                            Opcode = WS_OPCODE_PONG,
                            PayloadData = packet.PayloadData
                        });   
                }
                else if (opcode == WS_OPCODE_PONG)
                {
                    /* do nothing */
                }
                else if (opcode == WS_OPCODE_CLOSE)
                {
                    ushort code = WS_CLOSE_STATUS_NORMAL;
                    string reason = "";

                    if (packet.PayloadLength >= 2)
                    {
                        code = (ushort) ((packet.PayloadData[0] << 8) | packet.PayloadData[1]);
                    }

                    OnClose?.Invoke(this, true, code, reason);
                    Stop();
                }
                else
                {
                    OnClose?.Invoke(this, false, WS_CLOSE_STATUS_PROTOCOL_ERROR, "");
                    Stop();
                }

                lastOpCode = opcode;
            }
        }

        private async Task<WebSocketPacket> ReadPacketHeaderAsync()
        {
            byte[] mask = new byte[4];
            WebSocketPacket packet = new WebSocketPacket();

            byte[] hdrData = new byte[2];
            await ReadBytesAsync(hdrData, 2);

            packet.Fin = (hdrData[0] & WS_BIT_FIN) != 0;
            packet.Opcode = (byte) (hdrData[0] & WS_OPCODE_MASK);

            packet.Masked = (hdrData[1] & WS_BIT_MASK) != 0;
            long payloadSize = (hdrData[1] & WS_PAYLOAD_MASK);

            if (payloadSize == WS_PAYLOAD_16)
            {
                byte[] bytes16 = new byte[2];
                await ReadBytesAsync(bytes16, 2);
                payloadSize = BitConverter.ToUInt16(bytes16.Reverse().ToArray(), 0);
            }
            else if (payloadSize == WS_PAYLOAD_64)
            {
                byte[] bytes64 = new byte[8];
                await ReadBytesAsync(bytes64, 8);
                payloadSize = (long) BitConverter.ToUInt64(bytes64.Reverse().ToArray(), 0);
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

        private async Task<int> ReadBytesAsync(byte[] buffer, int count)
        {
            return await stream.ReadAsync(buffer, 0, count);
        }

        private void Send(WebSocketPacket packet)
        {            
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(memoryStream);

                packet.Masked = this.server ? false : true;

                byte[] mask = new byte[4];
                random.NextBytes(mask);

                writer.Write((byte)(WS_BIT_FIN | (byte) packet.Opcode));

                byte masked = (byte)(packet.Masked ? WS_BIT_MASK : 0);

                if (packet.PayloadLength < 126)
                {
                    writer.Write((byte)(masked | (byte) packet.PayloadLength));
                }
                else if (packet.PayloadLength <= 0xFFFF)
                {
                    byte[] bytes16 = BitConverter.GetBytes((UInt16) packet.PayloadLength).Reverse().ToArray();
                    writer.Write((byte)(masked | WS_PAYLOAD_16));
                    writer.Write(bytes16);
                }
                else
                {
                    byte[] bytes64 = BitConverter.GetBytes((UInt64) packet.PayloadLength).Reverse().ToArray();
                    writer.Write((byte)(masked | WS_PAYLOAD_64));
                    writer.Write(bytes64);
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
            public byte Opcode { get; set; }

            public bool Fin { get; set; }

            public bool Masked { get; set; }

            public byte[] PayloadData { get; set; }

            public int PayloadLength => PayloadData?.Length ?? 0;
        }
    }
}