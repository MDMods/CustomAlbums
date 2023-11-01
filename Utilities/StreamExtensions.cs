using System.Security.Cryptography;

namespace CustomAlbums.Utilities
{
    internal static class StreamExtensions
    {
        public static string GetHash(this MemoryStream stream)
        {
            var hash = MD5.Create().ComputeHash(stream.ToArray());
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public static byte[] ReadFully(this MemoryStream stream)
        {
            var buffer = new byte[1024*16];
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
