using System.IO.Compression;

namespace CustomAlbums.Utilities;

internal static class PackExtensions
{
    private static readonly Logger Logger = new(nameof(PackExtensions));

    public static ZipArchive GetNestedZip(this ZipArchive mdp, string entryName)
    {
        var mdm = mdp.GetEntry(entryName) ?? throw new ArgumentException($"Entry {entryName} not found.");
        var mdmStream = mdm.Open();
        var openedMdm = new ZipArchive(mdmStream, ZipArchiveMode.Read, false);
        return openedMdm;
    }
}