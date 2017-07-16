using System.IO;
using System.Threading.Tasks;

namespace MarginTrading.Services.Helpers
{
    public static class StreamHelpers
    {
        public static async Task<string> GetStreamPart(Stream stream, int maxPart)
        {
            stream.Seek(0, SeekOrigin.Begin);

            var reader = new StreamReader(stream);
            int len = (int)(stream.Length > maxPart ? maxPart : stream.Length);
            char[] bodyPart = new char[len];
            await reader.ReadAsync(bodyPart, 0, len);

            return new string(bodyPart);
        }
    }
}
