using System.Globalization;
using AMoverGRPC;
using MobileUser.Repositories.Interfaces;

namespace MobileUser.Repositories
{
    public class MotasRepository : IMotasRepository
    {
        private readonly object _sync = new();

        private UserProfile _profile = new UserProfile
        {
            Name = "Diana",
            Email = "diana@email.com",
            PhotoUri = "https://picsum.photos/200",
            Username = "diana.user"
        };

        private readonly DealershipInfo _dealership = new DealershipInfo
        {
            Name = "Stand Exemplo",
            Phone = "912345678",
            Email = "stand@email.com",
            Address = "Rua Exemplo, Vila Real",
            AssistancePhone = "932222222"
        };

        private readonly Dictionary<string, List<string>> _guests;
        private readonly List<AppNotification> _notifications;
        private readonly Dictionary<string, List<MaintenanceRecord>> _maintenance;

        public MotasRepository()
        {
            _guests = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["V-FG-2024-X1-001"] = new List<string> { "guest1@email.com", "guest2@email.com" },
                ["V-FG-2024-X1-002"] = new List<string> { "amigo@email.com" },
                ["V-FG-2024-X1-003"] = new List<string>()
            };

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

        public Task<UserProfile> GetUserProfileAsync()
        {
            lock (_sync)
            {
                return Task.FromResult(_profile.Clone());
            }
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

        public Task<ActionStatus> AddGuestAccessAsync(string vin, string guestEmail)
        {
            lock (_sync)
            {
                if (!IsValidEmail(guestEmail))
                {
                    return Task.FromResult(new ActionStatus
                    {
                        Success = false,
                        Message = "Email inválido."
                    });
                }

                if (!_guests.ContainsKey(vin))
                {
                    _guests[vin] = new List<string>();
                }

                var alreadyExists = _guests[vin]
                    .Any(email => string.Equals(email, guestEmail, StringComparison.OrdinalIgnoreCase));

                if (alreadyExists)
                {
                    return Task.FromResult(new ActionStatus
                    {
                        Success = false,
                        Message = $"O email '{guestEmail}' já tem acesso a esta mota."
                    });
                }

                _guests[vin].Add(guestEmail);

                return Task.FromResult(new ActionStatus
                {
                    Success = true,
                    Message = $"Acesso atribuído ao email {guestEmail} para a mota {vin}."
                });
            }
        }

        public Task<ActionStatus> RemoveGuestAccessAsync(string vin, string guestEmail)
        {
            lock (_sync)
            {
                if (!_guests.ContainsKey(vin))
                {
                    return Task.FromResult(new ActionStatus
                    {
                        Success = false,
                        Message = $"Não existem convidados para a mota '{vin}'."
                    });
                }

                var existingEmail = _guests[vin]
                    .FirstOrDefault(email => string.Equals(email, guestEmail, StringComparison.OrdinalIgnoreCase));

                if (existingEmail is null)
                {
                    return Task.FromResult(new ActionStatus
                    {
                        Success = false,
                        Message = $"O email '{guestEmail}' não tinha acesso a esta mota."
                    });
                }

                _guests[vin].Remove(existingEmail);

                return Task.FromResult(new ActionStatus
                {
                    Success = true,
                    Message = $"Acesso removido ao email {guestEmail} para a mota {vin}."
                });
            }
        }

        public Task<GuestListResponse> ListGuestAccessAsync(string vin)
        {
            lock (_sync)
            {
                var response = new GuestListResponse();

                if (_guests.TryGetValue(vin, out var list))
                {
                    response.GuestEmails.AddRange(list.OrderBy(email => email));
                }

                return Task.FromResult(response);
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

        public Task<ActionStatus> UpdateProfilePhotoAsync(byte[] imageData, string fileExtension)
        {
            lock (_sync)
            {
                if (imageData == null || imageData.Length == 0)
                {
                    return Task.FromResult(new ActionStatus
                    {
                        Success = false,
                        Message = "Dados da imagem estão vazios."
                    });
                }

                if (string.IsNullOrWhiteSpace(fileExtension))
                {
                    return Task.FromResult(new ActionStatus
                    {
                        Success = false,
                        Message = "Extensão do ficheiro é obrigatória."
                    });
                }

                var normalizedExtension = fileExtension.Trim().ToLowerInvariant();
                if (!normalizedExtension.StartsWith("."))
                    normalizedExtension = "." + normalizedExtension;

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!allowedExtensions.Contains(normalizedExtension))
                {
                    return Task.FromResult(new ActionStatus
                    {
                        Success = false,
                        Message = $"Extensão '{fileExtension}' não é permitida. Usa: .jpg, .jpeg, .png, .webp."
                    });
                }

                _profile.PhotoUri = $"https://picsum.photos/200?updated={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                return Task.FromResult(new ActionStatus
                {
                    Success = true,
                    Message = $"Foto de perfil atualizada ({imageData.Length} bytes, {normalizedExtension})."
                });
            }
        }

        public Task<ActionStatus> UpdateProfileInfoAsync(string name, string email)
        {
            lock (_sync)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Task.FromResult(new ActionStatus
                    {
                        Success = false,
                        Message = "Nome é obrigatório."
                    });
                }

                if (!IsValidEmail(email))
                {
                    return Task.FromResult(new ActionStatus
                    {
                        Success = false,
                        Message = "Email inválido."
                    });
                }

                _profile.Name = name.Trim();
                _profile.Email = email.Trim();

                return Task.FromResult(new ActionStatus
                {
                    Success = true,
                    Message = $"Perfil atualizado: {_profile.Name} / {_profile.Email}."
                });
            }
        }

        private static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var trimmed = email.Trim();

            if (trimmed.Contains(' '))
                return false;

            var atIndex = trimmed.IndexOf('@');
            return atIndex > 0 && atIndex < trimmed.Length - 1;
        }
    }
}
