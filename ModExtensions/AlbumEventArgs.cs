using CustomAlbums.Data;

namespace CustomAlbums.ModExtensions
{
    public class AlbumEventArgs : EventArgs
    {
        public Album Album;

        public AlbumEventArgs(Album album)
        {
            Album = album;
        }
    }
}
