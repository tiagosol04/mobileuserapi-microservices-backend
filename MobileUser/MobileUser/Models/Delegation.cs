using AMoverGRPC;

namespace MobileUser.Models
{
    public class Delegation
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();

        public string Vin { get; init; } = string.Empty;

        public string GuestEmail { get; init; } = string.Empty;

        public DelegationStatus Status { get; set; } = DelegationStatus.Pending;

        public string InviteToken { get; init; } = Guid.NewGuid().ToString("N");

        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }
}
