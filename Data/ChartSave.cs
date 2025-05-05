namespace CustomAlbums.Data
{
    public class ChartSave
    {
        public int Evaluate { get; set; } = 0;
        public int Score { get; set; } = 0;
        public int Combo { get; set; } = 0;
        public float Accuracy { get; set; } = 0f;
        public string AccuracyStr { get; set; } = "0";
        public float Clear { get; set; } = 0f;
        public int FailCount { get; set; } = 0;
        public bool Passed { get; set; } = false;
    }
}