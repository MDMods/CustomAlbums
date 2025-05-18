using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomAlbums.Utilities
{
    internal static class PackExtensions
    {
        private static readonly Logger Logger = new(nameof(PackExtensions));
        public static ZipArchive GetNestedZip(this ZipArchive mdp, string entryName)
        {
            var mdm = mdp.GetEntry(entryName);
            using var mdmStream = mdm.Open();
            var openedMdm = new ZipArchive(mdmStream);
            return openedMdm;
        }
    }
}
