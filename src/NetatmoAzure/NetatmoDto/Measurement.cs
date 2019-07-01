namespace NetatmoAzure
{
    public sealed class Measurement
    {
        public long Time_utc { get; set; }
        public double Temperature { get; set; }
        public int CO2 { get; set; }
        public int Humidity { get; set; }
        public int Noise { get; set; }
        public double Pressure { get; set; }
        public double AbsolutePressure { get; set; }
        public int Health_idx { get; set; }
        public double Min_temp { get; set; }
        public double Max_temp { get; set; }
        public long Date_min_temp { get; set; }
        public long Date_max_temp { get; set; }
    }
}