using CustomAlbums.Data;
using CustomAlbums.Utilities;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using UnityEngine;
using Logger = CustomAlbums.Utilities.Logger;

namespace CustomAlbums.Managers
{
    public static class CoverManager
    {
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

        public static AnimatedCover GetAnimatedCover(this Album album)
        {
            if (!album.HasFile("cover.gif")) return null;
            if (CachedAnimatedCovers.TryGetValue(album.Index, out var cached)) return cached;

            var stream = album.OpenFileStream("cover.gif");
            var image = GifDecoder.Instance.Decode<Rgba32>(new DecoderOptions(), stream);
            var sprites = new Sprite[image.Frames.Count];

            for (var i = 0; i < image.Frames.Count; i++)
            {
                var frameImage = image.Frames.CloneFrame(i);

                using var ms = new MemoryStream();
                frameImage.Save(ms, new PngEncoder());

                var tex = new Texture2D(frameImage.Width, frameImage.Height, TextureFormat.ARGB32, false);
                tex.LoadImage(ms.ToArray());

                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                sprites[i] = sprite;
            }

            var cover = new AnimatedCover(sprites, image.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay * 10);
            CachedAnimatedCovers.Add(album.Index, cover);

            return cover;
        }
    }
}