namespace CustomAlbums.Data
{
    internal class AlbumJson
    {
        internal string Uid { get; set; }
        internal string Title { get; set; }
        internal string PrefabsName { get; set; }
        internal string Price { get; set; }
        internal string JsonName { get; set; }
        internal bool NeedPurchase { get; set; }
        internal bool Free { get; set; }
    }

    internal class CustomChartJson
    {
        internal string Uid { get; set; }
        internal string Name { get; set; }
        internal string Author { get; set; }
        internal string Bpm { get; set; }
        internal string Music { get; set; }
        internal string Demo { get; set; }
        internal string Cover { get; set; }
        internal string NoteJson { get; set; }
        internal string Scene { get; set; }
        internal string UnlockLevel { get; set; }
        internal string LevelDesigner { get; set; }
        internal string LevelDesigner1 { get; set; }
        internal string LevelDesigner2 { get; set; }
        internal string LevelDesigner3 { get; set; }
        internal string LevelDesigner4 { get; set; }
        internal string Difficulty1 { get; set; }
        internal string Difficulty2 { get; set; }
        internal string Difficulty3 { get; set; }
        internal string Difficulty4 { get; set; }
    }
}