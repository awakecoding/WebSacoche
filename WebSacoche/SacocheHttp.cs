using System.Collections.Generic;
using System.Text;

namespace Sacoche
{
	public static class SacocheHttp
	{
		public static bool ParseRequestLine(string line, out string method, out string path, out string version)
		{
			method = "";
			path = "";
			version = "";

			if (line == null)
			{
				return false;
			}

			string[] parts = line.Split(' ');

			if (parts.Length != 3)
			{
				return false;
			}

			method = parts[0];
			path = parts[1];
			version = parts[2];

			return true;
		}

		public static bool ParseResponseLine(string line, out string version, out int code, out string reason)
        {
			version = "";
			code = 0;
			reason = "";

            if (line == null)
            {
                return false;
            }

            string[] parts = line.Split(' ');

            if (parts.Length < 3)
            {
                return false;
            }

			version = parts[0];

            if (!int.TryParse(parts[1], out code))
            {
                return false;
            }

            reason = string.Join(" ", parts, 2, parts.Length - 2);

            return true;
        }

		public static string GetFieldValue(string[] lines, string name)
		{
			string value = "";

			for (int i = 1; i < lines.Length; i++)
			{
				string line = lines[i];
				int colonIndex = line.IndexOf(':');

				if (colonIndex < 0)
					continue;
				
				string current = line.Substring(0, colonIndex).Trim();

				if (current == name)
				{
					value = line.Substring(colonIndex + 1).Trim();
					return value;
				}
			}

			return "";
		}
	}
}