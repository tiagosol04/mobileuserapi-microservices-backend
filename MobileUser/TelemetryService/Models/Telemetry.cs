namespace TelemetryService.Models
{
    public class Telemetry
    {
        public int Id { get; set; }
        public string Vin { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }

        public float BatteryLevel { get; set; }
        public float BatteryTemperature { get; set; }
        public float BatteryConsumption { get; set; }
        public float BatteryRange { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public float Speed { get; set; }
        public float AverageSpeed { get; set; }
        public float TotalKilometers { get; set; }

        public float TyreFront { get; set; }
        public float TyreBack { get; set; }

        public bool IsConnected { get; set; }
        public bool IsStarted { get; set; }
        public string DrivingMode { get; set; } = string.Empty;

        public float Voltage { get; set; }
        public float Current { get; set; }
        public float Power { get; set; }

        public string State { get; set; } = string.Empty;
    }
}