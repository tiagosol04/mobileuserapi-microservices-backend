namespace UserService.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PhotoUri { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class GuestAccess
    {
        public int Id { get; set; }
        public string Vin { get; set; } = string.Empty;
        public string OwnerUserId { get; set; } = string.Empty;
        public string GuestEmail { get; set; } = string.Empty;
        public string PermissionType { get; set; } = "viewer";
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
