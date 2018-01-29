using System;
using System.Security.Cryptography;
using System.Text;

namespace Sacoche
{
    public class SacocheWebResponse : SacocheWebMessage
    {
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

            stringBuilder.Append($"{Version} {Code} {Reason}\r\n");

            foreach (var header in Headers.GetKeys())
            {
                stringBuilder.Append($"{header}: {Headers.GetValue(header)}\r\n");
            }

            return stringBuilder.ToString();
        }
    }
}