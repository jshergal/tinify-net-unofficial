using System.Globalization;
using System.Net;

namespace Tinify.Unofficial
{
    public class TinifyException : System.Exception {
        internal static TinifyException Create(string message, string type, HttpStatusCode status)
        {
            return (int)status switch
            {
                401 or 429 => new AccountException(message, type, status),
                >= 400 and <= 499 => new ClientException(message, type, status),
                >= 500 and <= 599 => new ServerException(message, type, status),
                _ => new TinifyException(message, type, status)
            };
        }

        public HttpStatusCode Status { get; }

        internal TinifyException() : base() {}

        internal TinifyException(string message, System.Exception err = null) : base(message, err) { }

        internal TinifyException(string message, string type, HttpStatusCode status) :
            base($"{message} (HTTP {status:D}/{type})")
        {
            Status = status;
        }
    }

    public sealed class AccountException : TinifyException
    {
        internal AccountException() : base() {}

        internal AccountException(string message, System.Exception err = null) : base(message, err) { }

        internal AccountException(string message, string type, HttpStatusCode status) : base(message, type, status) { }
    }

    public sealed class ClientException : TinifyException
    {
        internal ClientException() : base() {}

        internal ClientException(string message, System.Exception err = null) : base(message, err) { }

        internal ClientException(string message, string type, HttpStatusCode status) : base(message, type, status) { }
    }

    public sealed class ServerException : TinifyException
    {
        internal ServerException() : base() {}

        internal ServerException(string message, System.Exception err = null) : base(message, err) { }

        internal ServerException(string message, string type, HttpStatusCode status) : base(message, type, status) { }
    }

    public sealed class ConnectionException : TinifyException
    {
        internal ConnectionException() : base() {}

        internal ConnectionException(string message, System.Exception err = null) : base(message, err) { }

        internal ConnectionException(string message, string type, HttpStatusCode status) : base(message, type, status) { }
    }
}
