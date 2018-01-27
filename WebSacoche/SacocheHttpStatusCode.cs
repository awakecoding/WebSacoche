using System.Collections.Generic;

namespace Sacoche
{
    public class SacocheHttpStatusCode
    {
        private static Dictionary<int, SacocheHttpStatusCode> codes = new Dictionary<int, SacocheHttpStatusCode>();
        
        public static readonly SacocheHttpStatusCode Continue = new SacocheHttpStatusCode(100, "Continue");
        public static readonly SacocheHttpStatusCode SwitchingProtocols = new SacocheHttpStatusCode(101, "Switching Protocols");
        public static readonly SacocheHttpStatusCode OK = new SacocheHttpStatusCode(200, "OK");
        public static readonly SacocheHttpStatusCode Created = new SacocheHttpStatusCode(201, "Created");
        public static readonly SacocheHttpStatusCode Accepted = new SacocheHttpStatusCode(202, "Accepted");
        public static readonly SacocheHttpStatusCode NonAuthoritativeInformation = new SacocheHttpStatusCode(203, "Non-Authoritative Information");
        public static readonly SacocheHttpStatusCode NoContent = new SacocheHttpStatusCode(204, "No Content");
        public static readonly SacocheHttpStatusCode ResetContent = new SacocheHttpStatusCode(205, "Reset Content");
        public static readonly SacocheHttpStatusCode PartialContent = new SacocheHttpStatusCode(206, "Partial Content");
        public static readonly SacocheHttpStatusCode MultipleChoices = new SacocheHttpStatusCode(300, "Multiple Choices");
        public static readonly SacocheHttpStatusCode MovedPermanently = new SacocheHttpStatusCode(301, "Moved Permanently");
        public static readonly SacocheHttpStatusCode Found = new SacocheHttpStatusCode(302, "Found");
        public static readonly SacocheHttpStatusCode SeeOther = new SacocheHttpStatusCode(303, "See Other");
        public static readonly SacocheHttpStatusCode NotModified = new SacocheHttpStatusCode(304, "Not Modified");
        public static readonly SacocheHttpStatusCode UseProxy = new SacocheHttpStatusCode(305, "Use Proxy");
        public static readonly SacocheHttpStatusCode TemporaryRedirect = new SacocheHttpStatusCode(307, "Temporary Redirect");
        public static readonly SacocheHttpStatusCode BadRequest = new SacocheHttpStatusCode(400, "Bad Request");
        public static readonly SacocheHttpStatusCode Unauthorized = new SacocheHttpStatusCode(401, "Unauthorized");
        public static readonly SacocheHttpStatusCode PaymentRequired = new SacocheHttpStatusCode(402, "Payment Required");
        public static readonly SacocheHttpStatusCode Forbidden = new SacocheHttpStatusCode(403, "Forbidden");
        public static readonly SacocheHttpStatusCode NotFound = new SacocheHttpStatusCode(404, "Not Found");
        public static readonly SacocheHttpStatusCode MethodNotAllowed = new SacocheHttpStatusCode(405, "Method Not Allowed");
        public static readonly SacocheHttpStatusCode NotAcceptable = new SacocheHttpStatusCode(406, "Not Acceptable");
        public static readonly SacocheHttpStatusCode ProxyAuthenticationRequired = new SacocheHttpStatusCode(407, "Proxy Authentication Required");
        public static readonly SacocheHttpStatusCode RequestTimeout = new SacocheHttpStatusCode(408, "Request Time-out");
        public static readonly SacocheHttpStatusCode Conflict = new SacocheHttpStatusCode(409, "Conflict");
        public static readonly SacocheHttpStatusCode Gone = new SacocheHttpStatusCode(410, "Gone");
        public static readonly SacocheHttpStatusCode LengthRequired = new SacocheHttpStatusCode(411, "Length Required");
        public static readonly SacocheHttpStatusCode PreconditionFailed = new SacocheHttpStatusCode(412, "Precondition Failed");
        public static readonly SacocheHttpStatusCode RequestEntityTooLarge = new SacocheHttpStatusCode(413, "Request Entity Too Large");
        public static readonly SacocheHttpStatusCode RequestURITooLarge = new SacocheHttpStatusCode(414, "Request-URI Too Large");
        public static readonly SacocheHttpStatusCode UnsupportedMediaType = new SacocheHttpStatusCode(415, "Unsupported Media Type");
        public static readonly SacocheHttpStatusCode RequestedRangeNotSatisfiable = new SacocheHttpStatusCode(416, "Requested range not satisfiable");
        public static readonly SacocheHttpStatusCode ExpectationFailed = new SacocheHttpStatusCode(417, "Expectation Failed");
        public static readonly SacocheHttpStatusCode InternalServerError = new SacocheHttpStatusCode(500, "Internal Server Error");
        public static readonly SacocheHttpStatusCode NotImplemented = new SacocheHttpStatusCode(501, "Not Implemented");
        public static readonly SacocheHttpStatusCode BadGateway = new SacocheHttpStatusCode(502, "Bad Gateway");
        public static readonly SacocheHttpStatusCode ServiceUnavailable = new SacocheHttpStatusCode(503, "Service Unavailable");
        public static readonly SacocheHttpStatusCode GatewayTimeout = new SacocheHttpStatusCode(504, "Gateway Time-out");
        public static readonly SacocheHttpStatusCode HTTPVersionNotSupported = new SacocheHttpStatusCode(505, "HTTP Version not supported");
        
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