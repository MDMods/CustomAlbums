namespace CustomAlbums.Data
{
    public readonly record struct BpmEntry(float Tick, float Freq) : IComparable<BpmEntry>
    {
        public int CompareTo(BpmEntry other) => Tick.CompareTo(other.Tick);
    }

    public readonly record struct TimeSigEntry(int Beat, float Percent);

    public readonly record struct RawNote(float Time, string Value, string Tone);

    public readonly record struct ProcessedNote(
        int Id,
        decimal Time,
        string NoteUid,
        decimal Length,
        int Pathway,
        bool Blood);
}
