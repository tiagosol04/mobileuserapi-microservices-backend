using UserService.Models;

namespace UserService.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(string userId);
        Task<UserActionResult> UpdateProfileInfoAsync(string userId, string name, string email);
        Task<UserActionResult> UpdateProfilePhotoAsync(string userId, byte[] imageData, string fileExtension);
        Task<UserActionResult> AddGuestAccessAsync(string vin, string ownerUserId, string guestEmail);
        Task<UserActionResult> RemoveGuestAccessAsync(string vin, string ownerUserId, string guestEmail);
        Task<List<GuestAccess>> ListGuestAccessAsync(string vin, string ownerUserId);
        Task<bool> UserHasAccessToVinAsync(string userId, string vin);
    }

    public record UserActionResult(bool Success, string Message);
}
