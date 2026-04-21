using AMoverGRPC;

namespace MobileUser.Repositories.Interfaces
{
    public interface IMotasRepository
    {
        Task<UserDataResponse> GetUserDataAsync();
        Task<MotaResponse?> GetMotaInfoAsync(string vin);

        Task<ActionStatus> AddGuestAccessAsync(string vin, string guestEmail);
        Task<ActionStatus> RemoveGuestAccessAsync(string vin, string guestEmail);
        Task<GuestListResponse> ListGuestAccessAsync(string vin);

        Task<NotificationResponse> GetNotificationsAsync();
        Task<ActionStatus> MarkNotificationAsReadAsync(string notificationId);

        Task<MaintenanceAgendaResponse> GetMaintenanceAgendaAsync(string vin);
        Task<ActionStatus> BookMaintenanceServiceAsync(int maintenanceId, string selectedDate);

        Task<ActionStatus> UpdateProfilePhotoAsync(byte[] imageData, string fileExtension);
        Task<ActionStatus> UpdateProfileInfoAsync(string name, string email);
    }

}
