using NotificationsService.Models;

namespace NotificationsService.Repositories.Interfaces
{
    public record NotificationResult(bool Success, string Message);

    public interface INotificationRepository
    {
        Task<List<Notification>> GetNotificationsAsync(string userId);
        Task<List<Notification>> GetUnreadNotificationsAsync(string userId);
        Task<(bool Found, bool Authorized, NotificationResult Result)> MarkAsReadAsync(string notificationId, string userId);
        Task<NotificationResult> CreateNotificationAsync(string userId, string vin, string title, string message, string type);
    }
}
