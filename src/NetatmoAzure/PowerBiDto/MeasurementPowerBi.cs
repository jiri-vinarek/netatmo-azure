using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetatmoAzure
{
    public sealed class MeasurementPowerBi
    {
        public string MacAddress { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public DateTime DateTimePrague { get; set; }
        public double Temperature { get; set; }
        public int CO2 { get; set; }
        public int Humidity { get; set; }
        public int Noise { get; set; }
        public double Pressure { get; set; }
    }
}
