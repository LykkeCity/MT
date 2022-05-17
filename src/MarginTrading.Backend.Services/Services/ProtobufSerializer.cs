// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace MarginTrading.Backend.Services.Services
{
    internal static class ProtoBufSerializer
    {
        public static T Deserialize<T>(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return ProtoBuf.Serializer.Deserialize<T>(stream);
            }
        }

        public static byte[] Serialize<T>(T data)
        {
            using (var stream = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(stream, data);
                stream.Flush();
                return stream.ToArray();
            }
        }
    }
}