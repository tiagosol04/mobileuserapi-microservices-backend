using FaultsService.Models;
using FaultsService.Repositories.Interfaces;

namespace FaultsService.Repositories
{
    // Dados mock em memória. Em produção substituir por base de dados real.
    public class FaultsRepository : IFaultsRepository
    {
        private readonly object _sync = new();
        private int _nextFaultId = 200;

        // Faults activos por VIN (inclui histórico — is_active distingue)
        private readonly Dictionary<string, List<FaultEntry>> _faultsByVin;

        public FaultsRepository()
        {
            var now = DateTime.UtcNow;

            _faultsByVin = new Dictionary<string, List<FaultEntry>>(StringComparer.OrdinalIgnoreCase)
            {
                ["V-FG-2024-X1-001"] = new List<FaultEntry>
                {
                    new()
                    {
                        Id = "f-001-w1",
                        Vin = "V-FG-2024-X1-001",
                        Code = "W-TYRE-001",
                        Title = "Pressão do pneu dianteiro baixa",
                        Description = "A pressão do pneu dianteiro está abaixo do valor recomendado (2.3 bar). Verifique e corrija antes de conduzir.",
                        Severity = "WARNING",
                        Timestamp = now.AddHours(-3),
                        IsActive = true,
                        IsAcknowledged = false
                    },
                    new()
                    {
                        Id = "f-001-i1",
                        Vin = "V-FG-2024-X1-001",
                        Code = "I-UPDATE-001",
                        Title = "Atualização de firmware disponível",
                        Description = "Existe uma nova versão de firmware (v2.1.4). Recomenda-se a atualização na próxima visita ao stand.",
                        Severity = "INFO",
                        Timestamp = now.AddDays(-2),
                        IsActive = false,
                        IsAcknowledged = true
                    }
                },
                ["V-FG-2024-X1-002"] = new List<FaultEntry>(),
                ["V-FG-2024-X1-003"] = new List<FaultEntry>
                {
                    new()
                    {
                        Id = "f-003-e1",
                        Vin = "V-FG-2024-X1-003",
                        Code = "E-BAT-001",
                        Title = "Falha na célula de bateria",
                        Description = "Detectada falha numa célula do pack de baterias. Capacidade total pode estar reduzida. Contacte o stand para diagnóstico.",
                        Severity = "ERROR",
                        Timestamp = now.AddHours(-1),
                        IsActive = true,
                        IsAcknowledged = false
                    },
                    new()
                    {
                        Id = "f-003-w1",
                        Vin = "V-FG-2024-X1-003",
                        Code = "W-MOTOR-001",
                        Title = "Temperatura do motor elevada",
                        Description = "A temperatura do motor excedeu 85°C durante a última condução. Recomenda-se descanso antes de nova utilização.",
                        Severity = "WARNING",
                        Timestamp = now.AddHours(-2),
                        IsActive = true,
                        IsAcknowledged = false
                    },
                    new()
                    {
                        Id = "f-003-w0",
                        Vin = "V-FG-2024-X1-003",
                        Code = "W-TYRE-001",
                        Title = "Pressão do pneu traseiro baixa",
                        Description = "A pressão do pneu traseiro estava abaixo do valor recomendado. Corrigido.",
                        Severity = "WARNING",
                        Timestamp = now.AddDays(-5),
                        IsActive = false,
                        IsAcknowledged = true
                    }
                }
            };
        }

        public Task<List<FaultEntry>> GetActiveFaultsAsync(string vin)
        {
            lock (_sync)
            {
                if (!_faultsByVin.TryGetValue(vin, out var all))
                    return Task.FromResult(new List<FaultEntry>());

                return Task.FromResult(
                    all.Where(f => f.IsActive)
                       .OrderByDescending(f => f.Severity == "ERROR")
                       .ThenByDescending(f => f.Timestamp)
                       .ToList());
            }
        }

        // GetFaultsByVin devolve todos os faults do VIN (activos e resolvidos), sem filtro adicional.
        public Task<List<FaultEntry>> GetFaultsByVinAsync(string vin)
        {
            lock (_sync)
            {
                if (!_faultsByVin.TryGetValue(vin, out var all))
                    return Task.FromResult(new List<FaultEntry>());

                return Task.FromResult(
                    all.OrderByDescending(f => f.Timestamp).ToList());
            }
        }

        public Task<List<FaultEntry>> GetFaultHistoryAsync(string vin)
        {
            lock (_sync)
            {
                if (!_faultsByVin.TryGetValue(vin, out var all))
                    return Task.FromResult(new List<FaultEntry>());

                return Task.FromResult(
                    all.OrderByDescending(f => f.Timestamp).ToList());
            }
        }

        public Task<List<FaultEntry>> GetWarningsAsync(string vin)
        {
            lock (_sync)
            {
                if (!_faultsByVin.TryGetValue(vin, out var all))
                    return Task.FromResult(new List<FaultEntry>());

                return Task.FromResult(
                    all.Where(f => f.IsActive && f.Severity == "WARNING")
                       .OrderByDescending(f => f.Timestamp)
                       .ToList());
            }
        }

        public Task<FaultActionResult> RegisterFaultAsync(
            string userId, string vin, string code, string title, string description, string severity)
        {
            lock (_sync)
            {
                if (!_faultsByVin.ContainsKey(vin))
                    _faultsByVin[vin] = new List<FaultEntry>();

                var normalizedSeverity = severity?.ToUpperInvariant() switch
                {
                    "ERROR" => "ERROR",
                    "WARNING" => "WARNING",
                    "INFO" => "INFO",
                    _ => "INFO"
                };

                var fault = new FaultEntry
                {
                    Id = $"f-{_nextFaultId++}",
                    Vin = vin,
                    Code = string.IsNullOrWhiteSpace(code) ? "UNKNOWN" : code,
                    Title = title,
                    Description = description,
                    Severity = normalizedSeverity,
                    Timestamp = DateTime.UtcNow,
                    IsActive = true,
                    IsAcknowledged = false
                };

                _faultsByVin[vin].Add(fault);

                return Task.FromResult(
                    new FaultActionResult(true, $"Fault registado para a mota '{vin}'.", fault.Id));
            }
        }

        public Task<FaultActionResult> ResolveFaultAsync(string userId, string vin, string faultId)
        {
            lock (_sync)
            {
                if (!_faultsByVin.TryGetValue(vin, out var all))
                    return Task.FromResult(
                        new FaultActionResult(false, $"Mota '{vin}' não reconhecida."));

                var fault = all.FirstOrDefault(f =>
                    f.Id.Equals(faultId, StringComparison.OrdinalIgnoreCase));

                if (fault is null)
                    return Task.FromResult(
                        new FaultActionResult(false, $"Fault '{faultId}' não encontrado."));

                if (!fault.IsActive)
                    return Task.FromResult(
                        new FaultActionResult(false, $"Fault '{faultId}' já estava resolvido."));

                fault.IsActive = false;
                fault.IsAcknowledged = true;
                return Task.FromResult(
                    new FaultActionResult(true, $"Fault '{faultId}' resolvido com sucesso.", faultId));
            }
        }

        public Task<FaultActionResult> AcknowledgeFaultAsync(string userId, string vin, string faultId)
        {
            lock (_sync)
            {
                if (!_faultsByVin.TryGetValue(vin, out var all))
                    return Task.FromResult(
                        new FaultActionResult(false, $"Mota '{vin}' não reconhecida."));

                var fault = all.FirstOrDefault(f =>
                    f.Id.Equals(faultId, StringComparison.OrdinalIgnoreCase));

                if (fault is null)
                    return Task.FromResult(
                        new FaultActionResult(false, $"Fault '{faultId}' não encontrado."));

                if (fault.IsAcknowledged)
                    return Task.FromResult(
                        new FaultActionResult(false, $"Fault '{faultId}' já foi reconhecido."));

                fault.IsAcknowledged = true;
                return Task.FromResult(
                    new FaultActionResult(true, $"Fault '{faultId}' reconhecido com sucesso."));
            }
        }
    }
}
