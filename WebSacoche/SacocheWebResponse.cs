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
    }
}