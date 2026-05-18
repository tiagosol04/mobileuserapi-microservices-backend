using FaultsService.Models;

namespace FaultsService.Repositories.Interfaces
{
    public interface IFaultsRepository
    {
        Task<List<FaultEntry>> GetActiveFaultsAsync(string vin);
        Task<List<FaultEntry>> GetFaultsByVinAsync(string vin);
        Task<List<FaultEntry>> GetFaultHistoryAsync(string vin);
        Task<List<FaultEntry>> GetWarningsAsync(string vin);
        Task<FaultActionResult> RegisterFaultAsync(string userId, string vin, string code, string title, string description, string severity);
        Task<FaultActionResult> ResolveFaultAsync(string userId, string vin, string faultId);
        Task<FaultActionResult> AcknowledgeFaultAsync(string userId, string vin, string faultId);
    }
}
