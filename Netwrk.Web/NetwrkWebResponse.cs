using System;
using System.Security.Cryptography;
using System.Text;

namespace Netwrk.Web
{
    public class NetwrkWebResponse : NetwrkWebMessage
    {
        public string Version { get; set; }
        public NetwrkHttpStatusCode StatusCode { get; set; }

        internal bool IsWebSocketAccepted =>
            StatusCode == NetwrkHttpStatusCode.SwitchingProtocols &&
            Headers[NetwrkKnownHttpHeaders.Connection] == "Upgrade" &&
            Headers[NetwrkKnownHttpHeaders.Upgrade] == "websocket" &&
            Headers.HasValue(NetwrkKnownHttpHeaders.SecWebSocketAccept);

        private bool ParseResponseLine(string line)
        {
            if (line == null)
            {
                return false;
            }

            string[] parts = line.Split(' ');

            if (parts.Length < 3)
            {
                return false;
            }

            Version = parts[0];

            if (!int.TryParse(parts[1], out var code))
            {
                return false;
            }
            else if (NetwrkHttpStatusCode.TryParse(code, out var statusCode))
            {
                StatusCode = statusCode;
            }
            else
            {
                StatusCode = new NetwrkHttpStatusCode(code, string.Join(" ", parts, 2, parts.Length - 2));
            }

            return true;
        }

        internal bool IsKeyValid(string requestKey)
        {
            using (var sha1 = SHA1.Create())
            {
                string accept = requestKey + NetwrkWebSocket.WS_MAGIC_GUID;
                byte[] acceptBytes = Encoding.UTF8.GetBytes(accept);
                byte[] acceptSha1 = sha1.ComputeHash(acceptBytes);

                return Headers[NetwrkKnownHttpHeaders.SecWebSocketAccept] == Convert.ToBase64String(acceptSha1);
            }
        }

        internal override bool Parse(string[] lines)
        {
            if (!ParseResponseLine(lines[0]))
            {
                return false;
            }

            return base.Parse(lines);
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{Version} {StatusCode.Code} {StatusCode.Status}");

            foreach (var header in Headers.GetKeys())
            {
                stringBuilder.AppendLine($"{header}: {Headers.GetValue(header)}");
            }

            return stringBuilder.ToString();
        }
    }
}