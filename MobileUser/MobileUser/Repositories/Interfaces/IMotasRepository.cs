using AMoverGRPC;

namespace MobileUser.Repositories.Interfaces
{
    public interface IMotasRepository
    {
        Task<DealershipInfo> GetDealershipInfoAsync();

        // TODO Fase 4C: mover para MaintenanceService
        Task<int> GetNextServiceKmAsync(string vin);
        Task<MaintenanceAgendaResponse> GetMaintenanceAgendaAsync(string vin);
        Task<ActionStatus> BookMaintenanceServiceAsync(string vin, int maintenanceId, string selectedDate);

        // TODO Fase 4B: mover para NotificationsService
        Task<NotificationResponse> GetNotificationsAsync();
        Task<ActionStatus> MarkNotificationAsReadAsync(string notificationId);
    }
}
