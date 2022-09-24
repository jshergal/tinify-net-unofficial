using System.Net.Http;
using System.Threading.Tasks;

/* We cannot and should not give a namespace and class the same name:
   https://msdn.microsoft.com/en-us/library/ms229026(v=vs.110).aspx */
namespace Tinify.Unofficial
{
    using Method = HttpMethod;

    public class Tinify
    {
        private static string key;
        private static string appIdentifier;
        private static string proxy;

        public static string Key
        {
            get
            {
                return key;
            }

            set
            {
                key = value;
            }
        }

        public static string AppIdentifier
        {
            get
            {
                return appIdentifier;
            }

            set
            {
                appIdentifier = value;
            }
        }

        public static string Proxy
        {
            get
            {
                return proxy;
            }

            set
            {
                proxy = value;
            }
        }

        public static uint? CompressionCount { get; set; }
    }
}