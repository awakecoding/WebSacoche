using System.Collections.Generic;
using System.Text;

namespace Netwrk.Web
{
	public class NetwrkWebRequest : NetwrkWebMessage
	{
		public string Method { get; set; }
		public string Path { get; set; }
		public string Version { get; set; }

		internal bool IsWebSocketRequest =>
			Method == "GET" &&
			Headers[NetwrkKnownHttpHeaders.Connection] == "Upgrade" &&
            Headers[NetwrkKnownHttpHeaders.Upgrade] == "websocket" &&
            Headers[NetwrkKnownHttpHeaders.SecWebSocketVersion] == "13" &&
			Headers.HasValue(NetwrkKnownHttpHeaders.SecWebSocketKey);
		
        public NetwrkWebRequest()
        {
            Method = "GET";
            Path = "/";
            Version = "HTTP/1.1";
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

        internal override bool Parse(string[] lines)
		{
			if (!ParseRequestLine(lines[0]))
			{
				return false;
			}

			return base.Parse(lines);
		}

		private bool ParseRequestLine(string line)
		{
			if (line == null)
			{
				return false;
			}

			string[] parts = line.Split(' ');

			if (parts.Length != 3)
			{
				return false;
			}

			Method = parts[0];
			Path = parts[1];
			Version = parts[2];

			return true;
		}
	}
}