﻿using System.IO;
using System.Threading.Tasks;

namespace MarginTrading.Common.Helpers
{
    public static class StreamHelpers
    {
        public static async Task<string> GetStreamPart(Stream stream, int maxPart)
        {
            stream.Seek(0, SeekOrigin.Begin);

            var reader = new StreamReader(stream);
            var len = (int)(stream.Length > maxPart ? maxPart : stream.Length);
            var bodyPart = new char[len];
            await reader.ReadAsync(bodyPart, 0, len);

            return new string(bodyPart);
        }
    }
}
