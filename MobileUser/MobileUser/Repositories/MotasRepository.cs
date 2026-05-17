using System.Globalization;
using AMoverGRPC;
using MobileUser.Repositories.Interfaces;

namespace MobileUser.Repositories
{
    // Repositório residual do BFF. Contém apenas lógica ainda não migrada para microserviços próprios.
    // Perfil, foto e guest access foram migrados para UserService (Fase 4A).
    // TODO Fase 4B: mover notificações para NotificationsService.
    // TODO Fase 4C: mover manutenção para MaintenanceService.
    public class MotasRepository : IMotasRepository
    {
        private readonly object _sync = new();

        private readonly DealershipInfo _dealership = new DealershipInfo
        {
            Name = "Stand Exemplo",
            Phone = "912345678",
            Email = "stand@email.com",
            Address = "Rua Exemplo, Vila Real",
            AssistancePhone = "932222222"
        };

        private readonly List<AppNotification> _notifications;
        private readonly Dictionary<string, List<MaintenanceRecord>> _maintenance;

        public MotasRepository()
        {
            _notifications = new List<AppNotification>
            {
                new AppNotification
                {
                    Id = "1",
                    Title = "Bateria fraca",
                    Message = "A Fulgora X1 Eco está com apenas 5% de bateria.",
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(-30).ToUnixTimeSeconds(),
                    IsRead = false
                },
                new AppNotification
                {
                    Id = "2",
                    Title = "Revisão próxima",
                    Message = "A Fulgora X1 está perto da próxima revisão aos 2000 km.",
                    Timestamp = DateTimeOffset.UtcNow.AddHours(-2).ToUnixTimeSeconds(),
                    IsRead = false
                },
                new AppNotification
                {
                    Id = "3",
                    Title = "Carregamento iniciado",
                    Message = "A Fulgora X1 Sport começou a carregar.",
                    Timestamp = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds(),
                    IsRead = true
                },
                new AppNotification
                {
                    Id = "4",
                    Title = "Mota desconectada",
                    Message = "A Fulgora X1 Eco perdeu ligação ao servidor.",
                    Timestamp = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds(),
                    IsRead = true
                }
            };

            _maintenance = new Dictionary<string, List<MaintenanceRecord>>(StringComparer.OrdinalIgnoreCase)
            {
                ["V-FG-2024-X1-001"] = new List<MaintenanceRecord>
                {
                    new MaintenanceRecord
                    {
                        Id = 1,
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
                    new MaintenanceRecord
                    {
                        Id = 2,
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
                    new MaintenanceRecord
                    {
                        Id = 3,
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
                    new MaintenanceRecord
                    {
                        Id = 4,
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

        public Task<DealershipInfo> GetDealershipInfoAsync()
        {
            lock (_sync)
            {
                return Task.FromResult(_dealership.Clone());
            }
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

        public Task<NotificationResponse> GetNotificationsAsync()
        {
            lock (_sync)
            {
                var response = new NotificationResponse();

                foreach (var notification in _notifications.OrderByDescending(n => n.Timestamp))
                {
                    response.Notifications.Add(notification.Clone());
                }

                return Task.FromResult(response);
            }
        }

        public Task<ActionStatus> MarkNotificationAsReadAsync(string notificationId)
        {
            lock (_sync)
            {
                var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);

                if (notification is null)
                {
                    return Task.FromResult(new ActionStatus
                    {
                        Success = false,
                        Message = $"Notificação '{notificationId}' não encontrada."
                    });
                }

                if (notification.IsRead)
                {
                    return Task.FromResult(new ActionStatus
                    {
                        Success = true,
                        Message = $"Notificação '{notificationId}' já estava marcada como lida."
                    });
                }

                notification.IsRead = true;

                return Task.FromResult(new ActionStatus
                {
                    Success = true,
                    Message = $"Notificação {notificationId} marcada como lida."
                });
            }
        }

        public Task<MaintenanceAgendaResponse> GetMaintenanceAgendaAsync(string vin)
        {
            lock (_sync)
            {
                var response = new MaintenanceAgendaResponse();

                if (_maintenance.TryGetValue(vin, out var records))
                {
                    foreach (var record in records.OrderBy(r => r.Date))
                    {
                        response.Maintenance.Add(record.Clone());
                    }
                }

                return Task.FromResult(response);
            }
        }

        public Task<ActionStatus> BookMaintenanceServiceAsync(string vin, int maintenanceId, string selectedDate)
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
                    return Task.FromResult(new ActionStatus
                    {
                        Success = false,
                        Message = "A data deve estar no formato yyyy-MM-dd."
                    });
                }

                if (!_maintenance.TryGetValue(vin, out var records))
                {
                    return Task.FromResult(new ActionStatus
                    {
                        Success = false,
                        Message = $"Não existe agenda de manutenção para a mota '{vin}'."
                    });
                }

                var record = records.FirstOrDefault(r => r.Id == maintenanceId);

                if (record is null)
                {
                    return Task.FromResult(new ActionStatus
                    {
                        Success = false,
                        Message = $"Registo de manutenção {maintenanceId} não encontrado."
                    });
                }

                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                record.Date = selectedDate;
                record.Status = MaintenanceStatus.Scheduled;
                record.DaysRemaining = Math.Max(parsedDate.DayNumber - today.DayNumber, 0);

                return Task.FromResult(new ActionStatus
                {
                    Success = true,
                    Message = $"Serviço '{record.Title}' agendado para {selectedDate}."
                });
            }
        }
    }
}
