using System.Collections.Generic;
using System.Text;

namespace Sacoche
{
	public class SacocheWebRequest : SacocheWebMessage
	{
		internal bool IsWebSocketRequest =>
			Method == "GET" &&
			Headers["Connection"] == "Upgrade" &&
            Headers["Upgrade"] == "websocket" &&
            Headers["Sec-WebSocket-Version"] == "13" &&
			Headers.HasValue("Sec-WebSocket-Key");
		
        public SacocheWebRequest()
        {
            Method = "GET";
            Path = "/";
            Version = "HTTP/1.1";
        }

		internal override bool Parse(string[] lines)
		{
			if (!ParseRequestLine(lines[0]))
			{
				return false;
			}

			return base.Parse(lines);
		}
        
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();

			stringBuilder.Append($"{Method} {Path} {Version}\r\n");

			foreach (var header in Headers.GetKeys())
			{
				stringBuilder.Append($"{header}: {Headers.GetValue(header)}\r\n");
			}
			
			return stringBuilder.ToString();
		}
	}
}