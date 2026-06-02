namespace ChargingService.Models
{
    public class ChargingStatus
    {
        public string Vin { get; set; } = string.Empty;
        public bool IsCharging { get; set; }
        public string ChargingTime { get; set; } = string.Empty;
        public string BatteryHealth { get; set; } = string.Empty;
        public int BatteryCycles { get; set; }
    }

    public class ChargingSession
    {
        public string Id { get; set; } = string.Empty;
        public string Vin { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int InitialSoc { get; set; }
        public int FinalSoc { get; set; }
        public bool IsCharging { get; set; }
        public string ChargingTime { get; set; } = string.Empty;
        public int BatteryCycles { get; set; }
        public float EnergyChargedKwh { get; set; }
        public string ChargerType { get; set; } = string.Empty;
        public string StartLocation { get; set; } = string.Empty;
        public string EndLocation { get; set; } = string.Empty;
        public int TotalTimeMinutes { get; set; }
        public bool IsActive { get; set; }
    }

    public record ChargingActionResult(bool Success, string Message, string SessionId = "");
}
