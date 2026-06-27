namespace CustomAlbums.Data;

public class BmsInfo
{
    /// <summary>BMS chart title (e.g. song name). Parsed from #TITLE header.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Artist name. Parsed from #ARTIST header.</summary>
    public string Artist { get; set; } = string.Empty;

    /// <summary>Scene identifier (e.g. "scene_08"). Parsed from #GENRE header.</summary>
    public string Genre { get; set; } = string.Empty;

    /// <summary>Banner image path. Parsed from #BANNER header, prefixed with "cover/".</summary>
    public string Banner { get; set; } = "cover/none_cover.png";

    /// <summary>Chart name. Set by the loader, not from a BMS header.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Player/speed lane mode. Parsed from #PLAYER header. Defaults to 1.</summary>
    public int Player { get; set; } = 1;

    /// <summary>Base BPM of the chart. Parsed from #BPM header.</summary>
    public float Bpm { get; set; }

    /// <summary>Difficulty rank (1-4). Parsed from #RANK header.</summary>
    public int Rank { get; set; }

    /// <summary>Long note type. Parsed from #LNTYPE header.</summary>
    public int LnType { get; set; }

    /// <summary>Level designer name. Parsed from #LEVELDESIGN header.</summary>
    public string LevelDesign { get; set; } = string.Empty;

    /// <summary>Play level. Parsed from #PLAYLEVEL header.</summary>
    public string PlayLevel { get; set; } = string.Empty;

    /// <summary>
    ///     Fallback storage for any unrecognized BMS headers.
    ///     Key is the header name (without #), value is the full string value.
    /// </summary>
    public Dictionary<string, string> Extra { get; } = new();
}