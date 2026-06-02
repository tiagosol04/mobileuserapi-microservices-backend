using ChargingService.Models;
using ChargingService.Repositories.Interfaces;

namespace ChargingService.Repositories
{
    // Dados mock em memória. Em produção substituir por base de dados real.
    public class ChargingRepository : IChargingRepository
    {
        private readonly object _sync = new();
        private int _nextSessionId = 100;

        // Estado de carregamento por VIN (inclui sessão activa para VIN 002)
        private readonly Dictionary<string, ChargingStatus> _statusByVin;

        // Histórico de sessões concluídas por VIN
        private readonly Dictionary<string, List<ChargingSession>> _historyByVin;

        // Sessões activas por VIN (no máximo uma por VIN)
        private readonly Dictionary<string, ChargingSession> _activeSessionByVin;

        public ChargingRepository()
        {
            _statusByVin = new Dictionary<string, ChargingStatus>(StringComparer.OrdinalIgnoreCase)
            {
                ["V-FG-2024-X1-001"] = new ChargingStatus
                {
                    Vin = "V-FG-2024-X1-001",
                    IsCharging = false,
                    ChargingTime = "",
                    BatteryHealth = "Good",
                    BatteryCycles = 42
                },
                ["V-FG-2024-X1-002"] = new ChargingStatus
                {
                    Vin = "V-FG-2024-X1-002",
                    IsCharging = true,
                    ChargingTime = "1h 12m",
                    BatteryHealth = "Good",
                    BatteryCycles = 108
                },
                ["V-FG-2024-X1-003"] = new ChargingStatus
                {
                    Vin = "V-FG-2024-X1-003",
                    IsCharging = false,
                    ChargingTime = "",
                    BatteryHealth = "Fair",
                    BatteryCycles = 312
                }
            };

            _historyByVin = new Dictionary<string, List<ChargingSession>>(StringComparer.OrdinalIgnoreCase)
            {
                ["V-FG-2024-X1-001"] = new List<ChargingSession>
                {
                    new()
                    {
                        Id = "1",
                        Vin = "V-FG-2024-X1-001",
                        StartTime = DateTime.UtcNow.AddDays(-3),
                        EndTime = DateTime.UtcNow.AddDays(-3).AddMinutes(72),
                        InitialSoc = 18,
                        FinalSoc = 100,
                        IsCharging = false,
                        ChargingTime = "1h 12m",
                        BatteryCycles = 41,
                        EnergyChargedKwh = 2.8f,
                        ChargerType = "AC",
                        StartLocation = "Vila Real",
                        EndLocation = "Vila Real",
                        TotalTimeMinutes = 72,
                        IsActive = false
                    },
                    new()
                    {
                        Id = "2",
                        Vin = "V-FG-2024-X1-001",
                        StartTime = DateTime.UtcNow.AddDays(-7),
                        EndTime = DateTime.UtcNow.AddDays(-7).AddMinutes(54),
                        InitialSoc = 35,
                        FinalSoc = 100,
                        IsCharging = false,
                        ChargingTime = "54m",
                        BatteryCycles = 40,
                        EnergyChargedKwh = 2.1f,
                        ChargerType = "AC",
                        StartLocation = "Vila Real",
                        EndLocation = "Vila Real",
                        TotalTimeMinutes = 54,
                        IsActive = false
                    }
                },
                ["V-FG-2024-X1-002"] = new List<ChargingSession>
                {
                    new()
                    {
                        Id = "3",
                        Vin = "V-FG-2024-X1-002",
                        StartTime = DateTime.UtcNow.AddDays(-2),
                        EndTime = DateTime.UtcNow.AddDays(-2).AddMinutes(90),
                        InitialSoc = 10,
                        FinalSoc = 100,
                        IsCharging = false,
                        ChargingTime = "1h 30m",
                        BatteryCycles = 107,
                        EnergyChargedKwh = 3.1f,
                        ChargerType = "DC",
                        StartLocation = "Porto",
                        EndLocation = "Porto",
                        TotalTimeMinutes = 90,
                        IsActive = false
                    },
                    new()
                    {
                        Id = "4",
                        Vin = "V-FG-2024-X1-002",
                        StartTime = DateTime.UtcNow.AddDays(-5),
                        EndTime = DateTime.UtcNow.AddDays(-5).AddMinutes(65),
                        InitialSoc = 22,
                        FinalSoc = 100,
                        IsCharging = false,
                        ChargingTime = "1h 05m",
                        BatteryCycles = 106,
                        EnergyChargedKwh = 2.6f,
                        ChargerType = "AC",
                        StartLocation = "Braga",
                        EndLocation = "Braga",
                        TotalTimeMinutes = 65,
                        IsActive = false
                    }
                },
                ["V-FG-2024-X1-003"] = new List<ChargingSession>
                {
                    new()
                    {
                        Id = "5",
                        Vin = "V-FG-2024-X1-003",
                        StartTime = DateTime.UtcNow.AddDays(-1),
                        EndTime = DateTime.UtcNow.AddDays(-1).AddMinutes(110),
                        InitialSoc = 5,
                        FinalSoc = 80,
                        IsCharging = false,
                        ChargingTime = "1h 50m",
                        BatteryCycles = 311,
                        EnergyChargedKwh = 2.3f,
                        ChargerType = "AC",
                        StartLocation = "Lisboa",
                        EndLocation = "Lisboa",
                        TotalTimeMinutes = 110,
                        IsActive = false
                    },
                    new()
                    {
                        Id = "6",
                        Vin = "V-FG-2024-X1-003",
                        StartTime = DateTime.UtcNow.AddDays(-4),
                        EndTime = DateTime.UtcNow.AddDays(-4).AddMinutes(95),
                        InitialSoc = 12,
                        FinalSoc = 90,
                        IsCharging = false,
                        ChargingTime = "1h 35m",
                        BatteryCycles = 310,
                        EnergyChargedKwh = 2.5f,
                        ChargerType = "DC",
                        StartLocation = "Setúbal",
                        EndLocation = "Setúbal",
                        TotalTimeMinutes = 95,
                        IsActive = false
                    }
                }
            };

            // VIN 002 está actualmente a carregar — sessão activa pré-populada
            _activeSessionByVin = new Dictionary<string, ChargingSession>(StringComparer.OrdinalIgnoreCase)
            {
                ["V-FG-2024-X1-002"] = new ChargingSession
                {
                    Id = "99",
                    Vin = "V-FG-2024-X1-002",
                    StartTime = DateTime.UtcNow.AddMinutes(-72),
                    InitialSoc = 17,
                    ChargerType = "AC",
                    StartLocation = "Casa",
                    IsActive = true
                }
            };
        }

        public Task<ChargingStatus> GetChargingStatusAsync(string vin)
        {
            lock (_sync)
            {
                if (_statusByVin.TryGetValue(vin, out var status))
                    return Task.FromResult(new ChargingStatus
                    {
                        Vin = status.Vin,
                        IsCharging = status.IsCharging,
                        ChargingTime = status.ChargingTime,
                        BatteryHealth = status.BatteryHealth,
                        BatteryCycles = status.BatteryCycles
                    });

                return Task.FromResult(new ChargingStatus { Vin = vin });
            }
        }

        public Task<List<ChargingSession>> GetChargingHistoryAsync(string vin)
        {
            lock (_sync)
            {
                if (!_historyByVin.TryGetValue(vin, out var sessions))
                    return Task.FromResult(new List<ChargingSession>());

                return Task.FromResult(sessions.OrderByDescending(s => s.StartTime).ToList());
            }
        }

        public Task<int> CalculateRemainingChargingTimeAsync(string vin)
        {
            lock (_sync)
            {
                if (!_statusByVin.TryGetValue(vin, out var status) || !status.IsCharging)
                    return Task.FromResult(0);

                // Mock: VIN 002 está a carregar desde há 72 minutos; estimativa de 48 minutos restantes
                if (_activeSessionByVin.TryGetValue(vin, out var active))
                {
                    var elapsed = (int)(DateTime.UtcNow - active.StartTime).TotalMinutes;
                    var estimated = Math.Max(120 - elapsed, 0);
                    return Task.FromResult(estimated);
                }

                return Task.FromResult(0);
            }
        }

        public Task<ChargingActionResult> StartChargingSessionAsync(
            string userId, string vin, string chargerType, string location, int initialSoc)
        {
            lock (_sync)
            {
                if (!_statusByVin.TryGetValue(vin, out var status))
                    return Task.FromResult(
                        new ChargingActionResult(false, $"Mota '{vin}' não reconhecida."));

                if (status.IsCharging)
                    return Task.FromResult(
                        new ChargingActionResult(false, $"A mota '{vin}' já está a carregar."));

                var sessionId = (_nextSessionId++).ToString();
                var session = new ChargingSession
                {
                    Id = sessionId,
                    Vin = vin,
                    StartTime = DateTime.UtcNow,
                    InitialSoc = initialSoc,
                    ChargerType = string.IsNullOrWhiteSpace(chargerType) ? "AC" : chargerType,
                    StartLocation = location,
                    IsActive = true
                };

                _activeSessionByVin[vin] = session;
                status.IsCharging = true;
                status.ChargingTime = "0m";

                return Task.FromResult(
                    new ChargingActionResult(true,
                        $"Sessão de carregamento iniciada para a mota '{vin}'.", sessionId));
            }
        }

        public Task<ChargingActionResult> EndChargingSessionAsync(
            string userId, string vin, string sessionId, int finalSoc)
        {
            lock (_sync)
            {
                if (!_activeSessionByVin.TryGetValue(vin, out var session))
                    return Task.FromResult(
                        new ChargingActionResult(false, $"Não existe sessão activa para a mota '{vin}'."));

                if (!string.IsNullOrWhiteSpace(sessionId) &&
                    !session.Id.Equals(sessionId, StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(
                        new ChargingActionResult(false,
                            $"Sessão '{sessionId}' não corresponde à sessão activa da mota '{vin}'."));

                session.EndTime = DateTime.UtcNow;
                session.FinalSoc = finalSoc;
                session.IsActive = false;
                session.TotalTimeMinutes = (int)(session.EndTime - session.StartTime).TotalMinutes;
                session.ChargingTime = FormatDuration(session.TotalTimeMinutes);
                session.EnergyChargedKwh = MathF.Round((finalSoc - session.InitialSoc) / 100f * 3.5f, 2);
                session.EndLocation = session.StartLocation;
                session.BatteryCycles = _statusByVin.TryGetValue(vin, out var s) ? s.BatteryCycles : 0;

                if (!_historyByVin.ContainsKey(vin))
                    _historyByVin[vin] = new List<ChargingSession>();
                _historyByVin[vin].Add(session);

                _activeSessionByVin.Remove(vin);

                if (_statusByVin.TryGetValue(vin, out var status))
                {
                    status.IsCharging = false;
                    status.ChargingTime = "";
                    status.BatteryCycles++;
                }

                return Task.FromResult(
                    new ChargingActionResult(true,
                        $"Sessão de carregamento terminada para a mota '{vin}'.", session.Id));
            }
        }

        private static string FormatDuration(int minutes)
        {
            if (minutes <= 0) return "0m";
            var h = minutes / 60;
            var m = minutes % 60;
            return h > 0 ? $"{h}h {m:D2}m" : $"{m}m";
        }
    }
}
