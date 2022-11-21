// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace MarginTrading.Common.Helpers
{
    public static class StreamHelpers
    {
        /// <summary>
        /// Copies the stream and reads a part of it. Assumes that the stream is seekable.
        /// Changes the source stream position to the beginning of the stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="maxBytes"></param>
        /// <returns></returns>
        public static async Task<byte[]> ReadBytes(this Stream stream, int maxBytes)
        {
            if ((stream?.Length ?? 0) == 0)
            {
                return Array.Empty<byte>();
            }
            
            if (maxBytes <= 0)
            {
                return Array.Empty<byte>();
            }
            
            if (!stream.CanSeek)
            {
                return Array.Empty<byte>();
            }

            return await Impl(stream, maxBytes);
        }

        private static async Task<byte[]> Impl(Stream stream, int maxBytes)
        {
            stream.Seek(0, SeekOrigin.Begin);
            try
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);

                ms.Seek(0, SeekOrigin.Begin);

                int bytesToRead = Math.Min((int)ms.Length, maxBytes);
                int bytesRead = 0;
                byte[] buffer = new byte[bytesToRead];
                do
                {
                    int read = await ms.ReadAsync(buffer, bytesRead, bytesToRead);
                    bytesRead += read;
                    bytesToRead -= read;
                } while (bytesToRead > 0);

                return buffer;
            }
            finally
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
        }
    }
}
