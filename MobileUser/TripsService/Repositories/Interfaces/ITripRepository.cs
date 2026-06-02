using TripsService.Models;

namespace TripsService.Repositories.Interfaces
{
    public interface ITripRepository
    {
        Task<Trip> StartTripAsync(string vin);
        Task<Trip?> EndTripAsync(string vin, double endLatitude, double endLongitude, float distanceKm, float energyConsumed, float averageSpeed);
        Task<List<Trip>> GetRecentTripsAsync(string vin);
        Task<Trip?> GetTripByIdAsync(int tripId);
        Task<List<Trip>> GetTripsByVinAsync(string vin);
        Task<float> GetTotalKilometersAsync(string vin);
    }
}