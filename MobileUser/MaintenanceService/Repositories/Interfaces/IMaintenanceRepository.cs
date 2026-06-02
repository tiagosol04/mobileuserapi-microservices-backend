using MaintenanceService.Models;

namespace MaintenanceService.Repositories.Interfaces
{
    public record MaintenanceActionResult(bool Success, string Message);

    public interface IMaintenanceRepository
    {
        Task<List<MaintenanceRecord>> GetMaintenanceAgendaAsync(string vin);
        Task<MaintenanceActionResult> BookMaintenanceServiceAsync(string vin, int maintenanceId, string selectedDate);
        Task<int> GetNextServiceKmAsync(string vin);
    }
}
