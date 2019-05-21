using Microsoft.WindowsAzure.Storage.Table;

namespace NetatmoAzure
{
    public class MeasurementTableEntity : TableEntity
    {
        public MeasurementTableEntity(string macAddress, Measurement measurement)
        {
            PartitionKey = macAddress;
            RowKey = $"{measurement.Time_utc:d19}";

            Temperature = measurement.Temperature;
            CO2 = measurement.CO2;
            Humidity = measurement.Humidity;
            Noise = measurement.Noise;
            Pressure = measurement.Pressure;
            AbsolutePressure = measurement.AbsolutePressure;
            Health_idx = measurement.Health_idx;
            Min_temp = measurement.Min_temp;
            Max_temp = measurement.Max_temp;
            Date_min_temp = measurement.Date_min_temp;
            Date_max_temp = measurement.Date_max_temp;
        }

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