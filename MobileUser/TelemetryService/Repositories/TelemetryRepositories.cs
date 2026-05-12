using TelemetryService.Models;
using TelemetryService.Repositories.Interfaces;

namespace TelemetryService.Repositories
{
    public class TelemetryRepository : ITelemetryRepository
    {
        private readonly object _sync = new();

        private readonly List<Telemetry> _telemetryRecords = new()
        {
            new Telemetry
            {
                Id = 1,
                Vin = "V-FG-2024-X1-001",
                Timestamp = DateTime.UtcNow,

                BatteryLevel = 87,
                BatteryTemperature = 24.6f,
                BatteryConsumption = 3.5f,
                BatteryRange = 120,

                Latitude = 41.2950,
                Longitude = -7.7460,

                Speed = 42,
                AverageSpeed = 39,
                TotalKilometers = 1540,

                TyreFront = 2.3f,
                TyreBack = 2.5f,

                IsConnected = true,
                IsStarted = false,
                DrivingMode = "NORMAL",

                Voltage = 52.4f,
                Current = 11.2f,
                Power = 586.8f,

                State = "ONLINE"
            }
        };

        public Task<Telemetry?> GetLatestTelemetryAsync(string vin)
        {
            lock (_sync)
            {
                var telemetry = _telemetryRecords
                    .Where(t => t.Vin == vin)
                    .OrderByDescending(t => t.Timestamp)
                    .FirstOrDefault();

                return Task.FromResult(telemetry);
            }
        }

        public Task<List<Telemetry>> GetTelemetryHistoryAsync(string vin, DateTime startDate, DateTime endDate)
        {
            lock (_sync)
            {
                var history = _telemetryRecords
                    .Where(t =>
                        t.Vin == vin &&
                        t.Timestamp >= startDate &&
                        t.Timestamp <= endDate)
                    .OrderByDescending(t => t.Timestamp)
                    .ToList();

                return Task.FromResult(history);
            }
        }

        public Task<Telemetry> SaveTelemetryAsync(Telemetry telemetry)
        {
            lock (_sync)
            {
                telemetry.Id = _telemetryRecords.Count + 1;
                telemetry.Timestamp = telemetry.Timestamp == default
                    ? DateTime.UtcNow
                    : telemetry.Timestamp;

                _telemetryRecords.Add(telemetry);

                return Task.FromResult(telemetry);
            }
        }

        public Task<bool> IsVehicleConnectedAsync(string vin)
        {
            lock (_sync)
            {
                var latest = _telemetryRecords
                    .Where(t => t.Vin == vin)
                    .OrderByDescending(t => t.Timestamp)
                    .FirstOrDefault();

                if (latest is null)
                    return Task.FromResult(false);

                var isRecentlySeen = latest.Timestamp >= DateTime.UtcNow.AddMinutes(-5);

                return Task.FromResult(latest.IsConnected && isRecentlySeen);
            }
        }

        public Task<DateTime?> GetLastSeenAsync(string vin)
        {
            lock (_sync)
            {
                var latest = _telemetryRecords
                    .Where(t => t.Vin == vin)
                    .OrderByDescending(t => t.Timestamp)
                    .FirstOrDefault();

                return Task.FromResult<DateTime?>(latest?.Timestamp);
            }
        }
    }
}