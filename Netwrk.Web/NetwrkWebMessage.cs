using System.Collections.Generic;
using System.Text;

namespace Netwrk.Web
{
	public abstract class NetwrkWebMessage
	{
		private byte[] data;

		internal NetwrkWebHeaderCollection Headers { get; } = new NetwrkWebHeaderCollection();

		public byte[] Data
		{
			get => data;
			set
			{
				data = value;
				Headers.SetValue(NetwrkKnownHttpHeaders.ContentLength, value?.Length.ToString());
			}
		}

		private bool ParseHeaderLine(string line)
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