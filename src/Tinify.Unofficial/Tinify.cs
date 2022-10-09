using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

/* We cannot and should not give a namespace and class the same name:
   https://msdn.microsoft.com/en-us/library/ms229026(v=vs.110).aspx */
namespace Tinify.Unofficial
{
    internal static class TinifyConstants
    {
        internal static JsonSerializerOptions SerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }
}