using TelemetryService.Models;

namespace TelemetryService.Repositories.Interfaces
{
    public interface ITelemetryRepository
    {
        Task<Telemetry?> GetLatestTelemetryAsync(string vin);
        Task<List<Telemetry>> GetTelemetryHistoryAsync(string vin, DateTime startDate, DateTime endDate);
        Task<Telemetry> SaveTelemetryAsync(Telemetry telemetry);
        Task<bool> IsVehicleConnectedAsync(string vin);
        Task<DateTime?> GetLastSeenAsync(string vin);
    }
}