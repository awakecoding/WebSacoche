using System;
using System.Security.Cryptography;
using System.Text;

namespace Sacoche
{
    public class SacocheWebResponse : SacocheWebMessage
    {
        public string Version { get; set; }
        public int Code { get; set; }
        public string Reason { get; set; }
        public SacocheHttpStatusCode StatusCode { get; set; }

        internal bool IsWebSocketAccepted =>
            StatusCode == SacocheHttpStatusCode.SwitchingProtocols &&
            Headers["Connection"] == "Upgrade" &&
            Headers["Upgrade"] == "websocket" &&
            Headers.HasValue("Sec-WebSocket-Accept");

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
            else if (SacocheHttpStatusCode.TryParse(code, out var statusCode))
            {
                Code = statusCode.Code;
                Reason = statusCode.Status;
                StatusCode = statusCode;
            }
            else
            {
                StatusCode = new SacocheHttpStatusCode(code, string.Join(" ", parts, 2, parts.Length - 2));
            }

            return true;
        }

        internal bool IsKeyValid(string requestKey)
        {
            using (var sha1 = SHA1.Create())
            {
                string accept = requestKey + SacocheWebSocket.WS_MAGIC_GUID;
                byte[] acceptBytes = Encoding.UTF8.GetBytes(accept);
                byte[] acceptSha1 = sha1.ComputeHash(acceptBytes);
                return Headers["Sec-WebSocket-Accept"] == Convert.ToBase64String(acceptSha1);
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

            stringBuilder.Append($"{Version} {StatusCode.Code} {StatusCode.Status}\r\n");

            foreach (var header in Headers.GetKeys())
            {
                stringBuilder.Append($"{header}: {Headers.GetValue(header)}\r\n");
            }

            return stringBuilder.ToString();
        }
    }
}