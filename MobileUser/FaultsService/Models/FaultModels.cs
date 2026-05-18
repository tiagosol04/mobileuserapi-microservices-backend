namespace FaultsService.Models
{
    public class FaultEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Vin { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsActive { get; set; }
        public bool IsAcknowledged { get; set; }
    }

    public record FaultActionResult(bool Success, string Message, string FaultId = "");
}
