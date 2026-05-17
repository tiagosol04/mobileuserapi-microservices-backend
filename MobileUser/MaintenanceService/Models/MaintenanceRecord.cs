namespace MaintenanceService.Models
{
    public enum MaintenanceStatus
    {
        Unknown = 0,
        DueSoon = 1,
        Scheduled = 2,
        Planned = 3,
        Completed = 4
    }

    public class MaintenanceRecord
    {
        public int Id { get; set; }
        public string Vin { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public int KmTrigger { get; set; }
        public int DaysRemaining { get; set; }
        public int KmRemaining { get; set; }
        public string ServiceCenter { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public MaintenanceStatus Status { get; set; }
    }
}
