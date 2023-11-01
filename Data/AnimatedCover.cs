using UnityEngine;

namespace CustomAlbums.Data
{
    public class AnimatedCover
    {
        public Sprite[] Frames { get; }
        public int FrameCount => Frames.Length;
        public int Width => Frames[0].texture.width;
        public int Height => Frames[0].texture.height;
        public int FramesPerSecond { get; }

        public AnimatedCover(Sprite[] frames, int framesPerSecond)
        {
            Frames = frames;
            FramesPerSecond = framesPerSecond;
        }
    }
}
