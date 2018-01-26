using System.Collections.Generic;

namespace Netwrk.Web
{
    public class NetwrkHttpStatusCode
    {
        private static Dictionary<int, NetwrkHttpStatusCode> codes = new Dictionary<int, NetwrkHttpStatusCode>();
        
        public static readonly NetwrkHttpStatusCode Continue = new NetwrkHttpStatusCode(100, "Continue");
        public static readonly NetwrkHttpStatusCode SwitchingProtocols = new NetwrkHttpStatusCode(101, "Switching Protocols");
        public static readonly NetwrkHttpStatusCode OK = new NetwrkHttpStatusCode(200, "OK");
        public static readonly NetwrkHttpStatusCode Created = new NetwrkHttpStatusCode(201, "Created");
        public static readonly NetwrkHttpStatusCode Accepted = new NetwrkHttpStatusCode(202, "Accepted");
        public static readonly NetwrkHttpStatusCode NonAuthoritativeInformation = new NetwrkHttpStatusCode(203, "Non-Authoritative Information");
        public static readonly NetwrkHttpStatusCode NoContent = new NetwrkHttpStatusCode(204, "No Content");
        public static readonly NetwrkHttpStatusCode ResetContent = new NetwrkHttpStatusCode(205, "Reset Content");
        public static readonly NetwrkHttpStatusCode PartialContent = new NetwrkHttpStatusCode(206, "Partial Content");
        public static readonly NetwrkHttpStatusCode MultipleChoices = new NetwrkHttpStatusCode(300, "Multiple Choices");
        public static readonly NetwrkHttpStatusCode MovedPermanently = new NetwrkHttpStatusCode(301, "Moved Permanently");
        public static readonly NetwrkHttpStatusCode Found = new NetwrkHttpStatusCode(302, "Found");
        public static readonly NetwrkHttpStatusCode SeeOther = new NetwrkHttpStatusCode(303, "See Other");
        public static readonly NetwrkHttpStatusCode NotModified = new NetwrkHttpStatusCode(304, "Not Modified");
        public static readonly NetwrkHttpStatusCode UseProxy = new NetwrkHttpStatusCode(305, "Use Proxy");
        public static readonly NetwrkHttpStatusCode TemporaryRedirect = new NetwrkHttpStatusCode(307, "Temporary Redirect");
        public static readonly NetwrkHttpStatusCode BadRequest = new NetwrkHttpStatusCode(400, "Bad Request");
        public static readonly NetwrkHttpStatusCode Unauthorized = new NetwrkHttpStatusCode(401, "Unauthorized");
        public static readonly NetwrkHttpStatusCode PaymentRequired = new NetwrkHttpStatusCode(402, "Payment Required");
        public static readonly NetwrkHttpStatusCode Forbidden = new NetwrkHttpStatusCode(403, "Forbidden");
        public static readonly NetwrkHttpStatusCode NotFound = new NetwrkHttpStatusCode(404, "Not Found");
        public static readonly NetwrkHttpStatusCode MethodNotAllowed = new NetwrkHttpStatusCode(405, "Method Not Allowed");
        public static readonly NetwrkHttpStatusCode NotAcceptable = new NetwrkHttpStatusCode(406, "Not Acceptable");
        public static readonly NetwrkHttpStatusCode ProxyAuthenticationRequired = new NetwrkHttpStatusCode(407, "Proxy Authentication Required");
        public static readonly NetwrkHttpStatusCode RequestTimeout = new NetwrkHttpStatusCode(408, "Request Time-out");
        public static readonly NetwrkHttpStatusCode Conflict = new NetwrkHttpStatusCode(409, "Conflict");
        public static readonly NetwrkHttpStatusCode Gone = new NetwrkHttpStatusCode(410, "Gone");
        public static readonly NetwrkHttpStatusCode LengthRequired = new NetwrkHttpStatusCode(411, "Length Required");
        public static readonly NetwrkHttpStatusCode PreconditionFailed = new NetwrkHttpStatusCode(412, "Precondition Failed");
        public static readonly NetwrkHttpStatusCode RequestEntityTooLarge = new NetwrkHttpStatusCode(413, "Request Entity Too Large");
        public static readonly NetwrkHttpStatusCode RequestURITooLarge = new NetwrkHttpStatusCode(414, "Request-URI Too Large");
        public static readonly NetwrkHttpStatusCode UnsupportedMediaType = new NetwrkHttpStatusCode(415, "Unsupported Media Type");
        public static readonly NetwrkHttpStatusCode RequestedRangeNotSatisfiable = new NetwrkHttpStatusCode(416, "Requested range not satisfiable");
        public static readonly NetwrkHttpStatusCode ExpectationFailed = new NetwrkHttpStatusCode(417, "Expectation Failed");
        public static readonly NetwrkHttpStatusCode InternalServerError = new NetwrkHttpStatusCode(500, "Internal Server Error");
        public static readonly NetwrkHttpStatusCode NotImplemented = new NetwrkHttpStatusCode(501, "Not Implemented");
        public static readonly NetwrkHttpStatusCode BadGateway = new NetwrkHttpStatusCode(502, "Bad Gateway");
        public static readonly NetwrkHttpStatusCode ServiceUnavailable = new NetwrkHttpStatusCode(503, "Service Unavailable");
        public static readonly NetwrkHttpStatusCode GatewayTimeout = new NetwrkHttpStatusCode(504, "Gateway Time-out");
        public static readonly NetwrkHttpStatusCode HTTPVersionNotSupported = new NetwrkHttpStatusCode(505, "HTTP Version not supported");
        
        public int Code { get; }

        public string Status { get; }

        internal NetwrkHttpStatusCode(int code, string status)
        {
            Code = code;
            Status = status;

            codes.Add(code, this);
        }
        
        public static bool TryParse(int code, out NetwrkHttpStatusCode statusCode) => codes.TryGetValue(code, out statusCode);
    }
}