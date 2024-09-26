namespace CustomAlbums.Utilities
{
    public static class StringExtensions
    {
        /// <summary>
        /// Compares two strings using the <see cref="StringComparison.OrdinalIgnoreCase"/> comparison type.
        /// </summary>
        /// <param name="str1">First string to compare</param>
        /// <param name="str2"></param>
        /// <returns><see langword="true"/> if the value of the <paramref name="str2"/> is the same as this string; otherwise, <see langword="false"/>.</returns>
        public static bool EqualsCaseInsensitive(this string str1, string str2) 
            => str1.Equals(str2, StringComparison.OrdinalIgnoreCase);
    }
}
