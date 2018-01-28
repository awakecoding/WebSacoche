using System.Collections.Generic;

namespace Sacoche
{
    public class SacocheHttpStatusCode
    {
        private static Dictionary<int, SacocheHttpStatusCode> codes = new Dictionary<int, SacocheHttpStatusCode>();
        
        public static readonly SacocheHttpStatusCode SwitchingProtocols = new SacocheHttpStatusCode(101, "Switching Protocols");
        public static readonly SacocheHttpStatusCode InternalServerError = new SacocheHttpStatusCode(500, "Internal Server Error");
 
        public int Code { get; }

        public string Status { get; }

        internal SacocheHttpStatusCode(int code, string status)
        {
            Code = code;
            Status = status;

            codes.Add(code, this);
        }
        
        public static bool TryParse(int code, out SacocheHttpStatusCode statusCode) => codes.TryGetValue(code, out statusCode);
    }
}