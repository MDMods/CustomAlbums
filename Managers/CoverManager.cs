using System.Runtime.CompilerServices;
using CustomAlbums.Data;
using CustomAlbums.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using UnityEngine;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Managers
{
    public static class CoverManager
    {
        internal static readonly Dictionary<int, Sprite> CachedCovers = new();
        internal static readonly Dictionary<int, AnimatedCover> CachedAnimatedCovers = new();
        private static readonly Logger Logger = new(nameof(CoverManager));

        private static readonly Configuration Config = Configuration.Default;

        public static Sprite GetCover(this Album album)
        {
            if (!album.HasPng) return null;
            if (CachedCovers.TryGetValue(album.Index, out var cached)) return cached;

            using var stream = album.OpenNullableStream("cover.png")?.ToMemoryStream();
            if (stream is null) return null;

            var bytes = stream.ReadFully();

            // Create the textures
            var texture = new Texture2D(2, 2, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.MirrorOnce
            };
            texture.LoadImage(bytes.MemCopyFromManaged());

            var cover = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            CachedCovers.Add(album.Index, cover);

            return cover;
        }

        public static unsafe AnimatedCover GetAnimatedCover(this Album album)
        {
            return null;
            // Early return statements
            if (!album.HasGif) return null;
            if (CachedAnimatedCovers.TryGetValue(album.Index, out var cached)) return cached;

            Config.PreferContiguousImageBuffers = true;

            // Open and load the gif
            using var stream = album.OpenNullableStream("cover.gif").ToMemoryStream();
            if (stream is null) return null;

            using var gif = Image.Load<Rgba32>(new DecoderOptions { Configuration = Config }, stream);

            // For some reason Unity loads textures upside down?
            // Flip the frames
            gif.Mutate(c => c.Flip(FlipMode.Vertical));

            var sprites = new Sprite[gif.Frames.Count];

            for (var i = 0; i < gif.Frames.Count; i++)
            {
                // Get frame data
                var frame = gif.Frames[i];
                var width = frame.Width;
                var height = frame.Height;

                // Get frame pixel data
                //
                // This should really be done with CopyPixelData and a byte array
                // but that causes a 6MB+ copy of an array that slows things down by a bit
                // The more efficient way is to retrieve an IntPtr that stores the data and pass that with a size instead
                var getPixelDataResult = frame.DangerousTryGetSinglePixelMemory(out var memory);
                if (!getPixelDataResult)
                {
                    Logger.Error("Failed to get pixel data.");
                    return null;
                }

                using var handle = memory.Pin();

                // Create the textures
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.MirrorOnce
                };
                texture.LoadRawTextureData((IntPtr)handle.Pointer, memory.Length * Unsafe.SizeOf<Rgba32>());
                texture.Apply(false);

                // Create the sprite with the given texture and add it to the sprites array
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
                sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                sprites[i] = sprite;
            }

            // Create and add cover to cache
            var cover = new AnimatedCover(sprites, gif.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay * 10);
            CachedAnimatedCovers.Add(album.Index, cover);

            return cover;
        }
    }
}