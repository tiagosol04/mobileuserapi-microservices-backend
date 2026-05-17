using ChargingService.Models;

namespace ChargingService.Repositories.Interfaces
{
    public interface IChargingRepository
    {
        Task<ChargingStatus> GetChargingStatusAsync(string vin);
        Task<List<ChargingSession>> GetChargingHistoryAsync(string vin);
        Task<int> CalculateRemainingChargingTimeAsync(string vin);
        Task<ChargingActionResult> StartChargingSessionAsync(string userId, string vin, string chargerType, string location, int initialSoc);
        Task<ChargingActionResult> EndChargingSessionAsync(string userId, string vin, string sessionId, int finalSoc);
    }
}
