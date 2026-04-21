using AMoverGRPC;
using MobileUser.Repositories.Interfaces;

namespace MobileUser.Repositories
{
    public class MotasRepository : IMotasRepository
    {
        public Task<UserDataResponse> GetUserDataAsync()
        {
            var response = new UserDataResponse
            {
                Profile = new UserProfile
                {
                    Name = "Diana",
                    Email = "diana@email.com",
                    PhotoUri = "https://exemplo.com/profile.jpg",
                    Username = "diana.user"
                },
                Dealership = new DealershipInfo
                {
                    Name = "Stand Exemplo",
                    Phone = "912345678",
                    Email = "stand@email.com",
                    Address = "Rua Exemplo, Vila Real",
                    AssistancePhone = "932222222"
                }
            };

            response.Bikes.Add(new MotaResponse
            {
                Vin = "V-FG-2024-X1-001",
                Name = "Fulgora X1",
                IsConnected = true,
                IsStarted = false,
                DrivingMode = DrivingMode.Normal,
                BatteryLevel = 87,
                IsCharging = false,
                BatteryHealth = "Boa",
                BatteryCycles = 45,
                BatteryTemperature = 24.6f,
                BatteryConsumption = 3.5f,
                BatteryRange = 120,
                ChargingTime = "1h20min",
                EnergyConsumptionAvg = 2.8f,
                AverageSpeed = 42,
                TyreFront = 2.3f,
                TyreBack = 2.5f,
                TotalKilometers = 1540,
                NextServiceKm = 2000,
                Latitude = 41.2950,
                Longitude = -7.7460
            });

            return Task.FromResult(response);
        }

        public Task<MotaResponse?> GetMotaInfoAsync(string vin)
        {
            if (vin != "V-FG-2024-X1-001")
                return Task.FromResult<MotaResponse?>(null);

            var response = new MotaResponse
            {
                Vin = vin,
                Name = "Fulgora X1",
                IsConnected = true,
                IsStarted = false,
                DrivingMode = DrivingMode.Normal,
                BatteryLevel = 87,
                IsCharging = false,
                BatteryHealth = "Boa",
                BatteryCycles = 45,
                BatteryTemperature = 24.6f,
                BatteryConsumption = 3.5f,
                BatteryRange = 120,
                ChargingTime = "1h20min",
                EnergyConsumptionAvg = 2.8f,
                AverageSpeed = 42,
                TyreFront = 2.3f,
                TyreBack = 2.5f,
                TotalKilometers = 1540,
                NextServiceKm = 2000,
                Latitude = 41.2950,
                Longitude = -7.7460
            };

            response.Documents.Add(new Document
            {
                Type = "manual",
                Uri = "https://exemplo.com/manual.pdf",
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });

            response.Documents.Add(new Document
            {
                Type = "insurance",
                Uri = "https://exemplo.com/insurance.pdf",
                UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });

            return Task.FromResult<MotaResponse?>(response);
        }

        public Task<ActionStatus> AddGuestAccessAsync(string vin, string guestEmail)
        {
            return Task.FromResult(new ActionStatus
            {
                Success = true,
                Message = $"Acesso atribuído ao email {guestEmail} para a mota {vin}."
            });
        }

        public Task<ActionStatus> RemoveGuestAccessAsync(string vin, string guestEmail)
        {
            return Task.FromResult(new ActionStatus
            {
                Success = true,
                Message = $"Acesso removido ao email {guestEmail} para a mota {vin}."
            });
        }

        public Task<GuestListResponse> ListGuestAccessAsync(string vin)
        {
            var response = new GuestListResponse();
            response.GuestEmails.Add("guest1@email.com");
            response.GuestEmails.Add("guest2@email.com");

            return Task.FromResult(response);
        }

        public Task<NotificationResponse> GetNotificationsAsync()
        {
            var response = new NotificationResponse();

            response.Notifications.Add(new AppNotification
            {
                Id = "1",
                Title = "Bateria fraca",
                Message = "A bateria está abaixo dos 20%.",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsRead = false
            });

            response.Notifications.Add(new AppNotification
            {
                Id = "2",
                Title = "Revisão próxima",
                Message = "A mota está perto da próxima revisão.",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsRead = true
            });

            return Task.FromResult(response);
        }

        public Task<ActionStatus> MarkNotificationAsReadAsync(string notificationId)
        {
            return Task.FromResult(new ActionStatus
            {
                Success = true,
                Message = $"Notificação {notificationId} marcada como lida."
            });
        }

        public Task<MaintenanceAgendaResponse> GetMaintenanceAgendaAsync(string vin)
        {
            var response = new MaintenanceAgendaResponse();

            response.Maintenance.Add(new MaintenanceRecord
            {
                Id = 1,
                Title = "Revisão Geral",
                Subtitle = "Manutenção periódica",
                Description = "Verificação completa da mota",
                Date = "2026-04-30",
                KmTrigger = 2000,
                DaysRemaining = 9,
                KmRemaining = 460,
                ServiceCenter = "Oficina Central",
                Contact = "912345678",
                Address = "Rua da Oficina, Vila Real",
                Status = MaintenanceStatus.DueSoon
            });

            return Task.FromResult(response);
        }

        public Task<ActionStatus> BookMaintenanceServiceAsync(int maintenanceId, string selectedDate)
        {
            return Task.FromResult(new ActionStatus
            {
                Success = true,
                Message = $"Serviço {maintenanceId} agendado para {selectedDate}."
            });
        }

        public Task<ActionStatus> UpdateProfilePhotoAsync(byte[] imageData, string fileExtension)
        {
            return Task.FromResult(new ActionStatus
            {
                Success = true,
                Message = $"Foto de perfil atualizada com ficheiro {fileExtension}."
            });
        }

        public Task<ActionStatus> UpdateProfileInfoAsync(string name, string email)
        {
            return Task.FromResult(new ActionStatus
            {
                Success = true,
                Message = $"Perfil atualizado: {name} / {email}."
            });

        }
    }
}