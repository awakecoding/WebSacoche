using System.Collections.Generic;
using System.Text;

namespace Sacoche
{
	public class SacocheWebRequest : SacocheWebMessage
	{
		internal override bool Parse(string[] lines)
		{
			if (!ParseRequestLine(lines[0]))
			{
				return false;
			}

			return base.Parse(lines);
		}
	}
}