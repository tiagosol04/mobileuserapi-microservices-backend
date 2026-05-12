using TripsService.Models;
using TripsService.Repositories.Interfaces;

namespace TripsService.Repositories
{
    public class TripRepository : ITripRepository
    {
        private readonly List<Trip> _trips = new()
        {
            new Trip
            {
                Id = 1,
                Vin = "V-FG-2024-X1-001",
                Name = "Viagem Vila Real",
                StartTime = DateTime.UtcNow.AddHours(-2),
                EndTime = DateTime.UtcNow.AddHours(-1),
                DistanceKm = 32.5f,
                EnergyConsumed = 4.2f,
                EnergyConsumptionAvg = 12.9f,
                AverageSpeed = 41.5f,
                StartLatitude = 41.2950,
                StartLongitude = -7.7460,
                EndLatitude = 41.3000,
                EndLongitude = -7.7300,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                IsActive = false
            }
        };

        public Task<Trip> StartTripAsync(string vin)
        {
            var trip = new Trip
            {
                Id = _trips.Count + 1,
                Vin = vin,
                Name = $"Viagem {_trips.Count + 1}",
                StartTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                StartLatitude = 41.2950,
                StartLongitude = -7.7460,
                IsActive = true
            };

            _trips.Add(trip);

            return Task.FromResult(trip);
        }

        public Task<Trip?> EndTripAsync(
            string vin,
            double endLatitude,
            double endLongitude,
            float distanceKm,
            float energyConsumed,
            float averageSpeed)
        {
            var trip = _trips
                .Where(t => t.Vin == vin && t.IsActive)
                .OrderByDescending(t => t.StartTime)
                .FirstOrDefault();

            if (trip is null)
                return Task.FromResult<Trip?>(null);

            trip.EndTime = DateTime.UtcNow;
            trip.EndLatitude = endLatitude;
            trip.EndLongitude = endLongitude;
            trip.DistanceKm = distanceKm;
            trip.EnergyConsumed = energyConsumed;
            trip.AverageSpeed = averageSpeed;
            trip.EnergyConsumptionAvg = distanceKm > 0
                ? energyConsumed / distanceKm * 100
                : 0;
            trip.IsActive = false;

            return Task.FromResult<Trip?>(trip);
        }

        public Task<List<Trip>> GetRecentTripsAsync(string vin)
        {
            var trips = _trips
                .Where(t => t.Vin == vin)
                .OrderByDescending(t => t.StartTime)
                .Take(10)
                .ToList();

            return Task.FromResult(trips);
        }

        public Task<Trip?> GetTripByIdAsync(int tripId)
        {
            var trip = _trips.FirstOrDefault(t => t.Id == tripId);
            return Task.FromResult(trip);
        }

        public Task<List<Trip>> GetTripsByVinAsync(string vin)
        {
            var trips = _trips
                .Where(t => t.Vin == vin)
                .ToList();

            return Task.FromResult(trips);
        }

        public Task<float> GetTotalKilometersAsync(string vin)
        {
            var total = _trips
                .Where(t => t.Vin == vin)
                .Sum(t => t.DistanceKm);

            return Task.FromResult(total);
        }
    }
}