namespace CustomAlbums.ModExtensions
{ 
    public class AssetEventArgs : EventArgs
    {
        public string AssetName;
        public IntPtr AssetPtr;

        public AssetEventArgs(string assetName, IntPtr assetPtr)
        {
            AssetName = assetName;
            AssetPtr = assetPtr;
        }
    }
}
