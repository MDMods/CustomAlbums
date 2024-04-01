﻿using System.Security.Cryptography;

namespace CustomAlbums.Utilities
{
    internal static class StreamExtensions
    {
        public static string GetHash(this Stream stream)
        {
            byte[] hash;
            if (stream is MemoryStream ms)
                hash = MD5.Create().ComputeHash(ms.ToArray());
            else
                hash = MD5.Create().ComputeHash(stream.ToMemoryStream().ToArray());
            
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static byte[] ReadFully(this MemoryStream stream)
        {
            var buffer = new byte[1024 * 16];
            int read;

            using var ms = new MemoryStream();
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                ms.Write(buffer, 0, read);

            return ms.ToArray();
        }
        public static MemoryStream ToMemoryStream(this Stream stream)
        {
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;

            return ms;
        }
    }
}