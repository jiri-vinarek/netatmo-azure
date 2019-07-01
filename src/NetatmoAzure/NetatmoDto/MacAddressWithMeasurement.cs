namespace NetatmoAzure
{
    public sealed class MacAddressWithMeasurement
    {
        public MacAddressWithMeasurement(string macAddress, Measurement measurement)
        {
            MacAddress = macAddress;
            Measurement = measurement;
        }

        public string MacAddress { get; }
        public Measurement Measurement { get; }
    }
}
