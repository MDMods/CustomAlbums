using CustomAlbums.Data;
using CustomAlbums.Utilities;
using Il2Cpp;
using Il2CppAssets.Scripts.PeroTools.Commons;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using UnityEngine;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Managers
{
    public static class CoverManager
    {
        private static readonly Configuration CustomConfig = Configuration.Default.Clone();
        private static readonly Dictionary<int, Sprite> CachedCovers = new();
        private static readonly Dictionary<int, AnimatedCover> CachedAnimatedCovers = new();
        private static readonly Logger Logger = new(nameof(CoverManager));

        public static Sprite GetCover(this Album album)
        {
            if (!album.HasFile("cover.png")) return null;
            if (CachedCovers.TryGetValue(album.Index, out var cached)) return cached;

            using var stream = album.OpenFileStream("cover.png");

            var bytes = stream.ReadFully();

            var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            tex.LoadImage(bytes);

            var cover = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            CachedCovers.Add(album.Index, cover);

            return cover;
        }

        public static unsafe AnimatedCover GetAnimatedCover(this Album album)
        {
            CustomConfig.PreferContiguousImageBuffers = true;

            if (!album.HasFile("cover.gif")) return null;
            if (CachedAnimatedCovers.TryGetValue(album.Index, out var cached)) return cached;

            using var stream = album.OpenFileStream("cover.gif");
            using var gif = Image.Load<Rgba32>(new DecoderOptions { Configuration = CustomConfig }, stream);

            gif.Mutate(processor => processor.Flip(FlipMode.Vertical));

            var sprites = new Sprite[gif.Frames.Count];

            for (var i = 0; i < gif.Frames.Count; i++)
            {
                var width = gif.Frames[i].Width;
                var height = gif.Frames[i].Height;

                var success = gif.Frames[i].DangerousTryGetSinglePixelMemory(out var memory);
                
                if (!success) 
                    Logger.Error("Failed to get pixel memory.");
                              
                using var handle = memory.Pin();
                
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.LoadRawTextureData((IntPtr)handle.Pointer, memory.Length * 4);
                tex.Apply(false, true);

                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                sprite.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                sprites[i] = sprite;
            }
            var cover = new AnimatedCover(sprites, gif.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay * 10);
            CachedAnimatedCovers.Add(album.Index, cover);

            return cover;
        }
    }
}