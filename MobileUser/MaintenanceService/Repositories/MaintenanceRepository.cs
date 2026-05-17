using System.Globalization;
using MaintenanceService.Models;
using MaintenanceService.Repositories.Interfaces;

namespace MaintenanceService.Repositories
{
    // Dados mock em memória. Em produção substituir por base de dados real.
    public class MaintenanceRepository : IMaintenanceRepository
    {
        private readonly object _sync = new();

        private readonly Dictionary<string, List<MaintenanceRecord>> _maintenance;

        public MaintenanceRepository()
        {
            _maintenance = new Dictionary<string, List<MaintenanceRecord>>(StringComparer.OrdinalIgnoreCase)
            {
                ["V-FG-2024-X1-001"] = new List<MaintenanceRecord>
                {
                    new()
                    {
                        Id = 1,
                        Vin = "V-FG-2024-X1-001",
                        Title = "Revisão Geral",
                        Subtitle = "Manutenção periódica",
                        Description = "Verificação completa da mota incluindo travões, pneus e sistema elétrico.",
                        Date = "2026-04-30",
                        KmTrigger = 2000,
                        DaysRemaining = 9,
                        KmRemaining = 460,
                        ServiceCenter = "Oficina Central",
                        Contact = "912345678",
                        Address = "Rua da Oficina, Vila Real",
                        Status = MaintenanceStatus.DueSoon
                    },
                    new()
                    {
                        Id = 2,
                        Vin = "V-FG-2024-X1-001",
                        Title = "Troca de Pneus",
                        Subtitle = "Desgaste previsto",
                        Description = "Substituição dos pneus dianteiro e traseiro.",
                        Date = "2026-06-15",
                        KmTrigger = 3000,
                        DaysRemaining = 55,
                        KmRemaining = 1460,
                        ServiceCenter = "Oficina Central",
                        Contact = "912345678",
                        Address = "Rua da Oficina, Vila Real",
                        Status = MaintenanceStatus.Planned
                    }
                },
                ["V-FG-2024-X1-002"] = new List<MaintenanceRecord>
                {
                    new()
                    {
                        Id = 3,
                        Vin = "V-FG-2024-X1-002",
                        Title = "Revisão Geral",
                        Subtitle = "Revisão dos 9000 km",
                        Description = "Inspeção completa do motor e BMS.",
                        Date = "2026-05-10",
                        KmTrigger = 9000,
                        DaysRemaining = 19,
                        KmRemaining = 280,
                        ServiceCenter = "Oficina Central",
                        Contact = "912345678",
                        Address = "Rua da Oficina, Vila Real",
                        Status = MaintenanceStatus.Scheduled
                    }
                },
                ["V-FG-2024-X1-003"] = new List<MaintenanceRecord>
                {
                    new()
                    {
                        Id = 4,
                        Vin = "V-FG-2024-X1-003",
                        Title = "Substituição Bateria",
                        Subtitle = "Bateria degradada",
                        Description = "Substituição da bateria principal por desgaste excessivo.",
                        Date = "2026-05-01",
                        KmTrigger = 15500,
                        DaysRemaining = 10,
                        KmRemaining = 300,
                        ServiceCenter = "Oficina Central",
                        Contact = "912345678",
                        Address = "Rua da Oficina, Vila Real",
                        Status = MaintenanceStatus.DueSoon
                    }
                }
            };
        }

        public Task<int> GetNextServiceKmAsync(string vin)
        {
            lock (_sync)
            {
                if (!_maintenance.TryGetValue(vin, out var records))
                    return Task.FromResult(0);

                var next = records
                    .Where(r => r.Status != MaintenanceStatus.Completed)
                    .OrderBy(r => r.KmTrigger)
                    .FirstOrDefault();

                return Task.FromResult(next?.KmTrigger ?? 0);
            }
        }

        public Task<List<MaintenanceRecord>> GetMaintenanceAgendaAsync(string vin)
        {
            lock (_sync)
            {
                if (!_maintenance.TryGetValue(vin, out var records))
                    return Task.FromResult(new List<MaintenanceRecord>());

                var result = records.OrderBy(r => r.Date).ToList();
                return Task.FromResult(result);
            }
        }

        public Task<MaintenanceActionResult> BookMaintenanceServiceAsync(string vin, int maintenanceId, string selectedDate)
        {
            lock (_sync)
            {
                if (!DateOnly.TryParseExact(
                        selectedDate,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedDate))
                {
                    return Task.FromResult(
                        new MaintenanceActionResult(false, "A data deve estar no formato yyyy-MM-dd."));
                }

                if (!_maintenance.TryGetValue(vin, out var records))
                {
                    return Task.FromResult(
                        new MaintenanceActionResult(false,
                            $"Não existe agenda de manutenção para a mota '{vin}'."));
                }

                var record = records.FirstOrDefault(r => r.Id == maintenanceId);

                if (record is null)
                {
                    return Task.FromResult(
                        new MaintenanceActionResult(false,
                            $"Registo de manutenção {maintenanceId} não encontrado."));
                }

                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                record.Date = selectedDate;
                record.Status = MaintenanceStatus.Scheduled;
                record.DaysRemaining = Math.Max(parsedDate.DayNumber - today.DayNumber, 0);

                return Task.FromResult(
                    new MaintenanceActionResult(true,
                        $"Serviço '{record.Title}' agendado para {selectedDate}."));
            }
        }
    }
}
