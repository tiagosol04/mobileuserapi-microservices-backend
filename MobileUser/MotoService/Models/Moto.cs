namespace MotoService.Models
{
    public class Moto
    {
        public int Id { get; set; }
        public string Vin { get; set; } = string.Empty;
        public string MotoSN { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public float BatteryCapacity { get; set; }
        public string ImageUri { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string UserId { get; set; } = string.Empty;
        public List<MotoDocumentModel> Documents { get; set; } = new();
    }

    public class MotoDocumentModel
    {
        public string Type { get; set; } = string.Empty;
        public string Uri { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}