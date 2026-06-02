namespace TripsService.Models
{
    public class Trip
    {
        public int Id { get; set; }
        public string Vin { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public float DistanceKm { get; set; }
        public float EnergyConsumed { get; set; }
        public float EnergyConsumptionAvg { get; set; }
        public float AverageSpeed { get; set; }

        public double StartLatitude { get; set; }
        public double StartLongitude { get; set; }
        public double EndLatitude { get; set; }
        public double EndLongitude { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}