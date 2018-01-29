using System.Collections.Generic;
using System.Text;

namespace Sacoche
{
	public abstract class SacocheWebMessage
	{
		private byte[] data;

		public string Method { get; set; }
		public string Path { get; set; }
		public string Version { get; set; }
		public int Code { get; set; }
        public string Reason { get; set; }

		internal SacocheWebHeaderCollection Headers { get; } = new SacocheWebHeaderCollection();

		public byte[] Data
		{
			get => data;
			set
			{
				data = value;
				Headers.SetValue("Content-Length", value?.Length.ToString());
			}
		}

		protected bool ParseRequestLine(string line)
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

		protected bool ParseResponseLine(string line)
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

            Code = code; 
            Reason = string.Join(" ", parts, 2, parts.Length - 2);

            return true;
        }
		
		protected bool ParseHeaderLine(string line)
		{
			if (line == null)
			{
				return false;
			}

			int colonIndex = line.IndexOf(':');

			if (colonIndex < 0)
			{
				return false;
			}

			string name = line.Substring(0, colonIndex).Trim();
			string value = line.Substring(colonIndex + 1).Trim();
			string[] values = GetFieldValues(value);

			Headers.AddValues(name, values);

			return true;
		}

        internal virtual bool Parse(string[] lines)
		{
			for (int i = 1; i < lines.Length; i++)
			{
				if (!ParseHeaderLine(lines[i]))
				{
					return false;
				}
			}

			return true;
		}

		private static string[] GetFieldValues(string value)
		{
			List<string> values = new List<string>();

			bool inString = false;
			StringBuilder currentString = new StringBuilder();

			void Emit()
			{
				if (currentString.Length > 0)
				{
					values.Add(currentString.ToString().Trim());
					currentString.Clear();
				}
			}

			foreach (var chr in value)
			{
				if (chr == '\"')
				{
					Emit();

					inString = !inString;
				}
				else
				{
					if (inString)
					{
						currentString.Append(chr);
					}
					else
					{
						if (chr == ',')
						{
							Emit();
						}
						else
						{
							currentString.Append(chr);
						}
					}
				}
			}

			Emit();

			return values.ToArray();
		}
	}
}