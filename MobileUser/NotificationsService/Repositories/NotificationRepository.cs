using NotificationsService.Models;
using NotificationsService.Repositories.Interfaces;

namespace NotificationsService.Repositories
{
    // Dados mock em memória. Em produção substituir por base de dados real.
    public class NotificationRepository : INotificationRepository
    {
        private readonly object _sync = new();
        private int _nextId = 10;

        private readonly List<Notification> _notifications;

        public NotificationRepository()
        {
            _notifications = new List<Notification>
            {
                // Diana — alerta de bateria para mota 001
                new Notification
                {
                    Id = "1",
                    UserId = "user-diana-001",
                    Vin = "V-FG-2024-X1-001",
                    Title = "Bateria fraca",
                    Message = "A Fulgora X1 está com apenas 15% de bateria.",
                    Type = "alert",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-30)
                },
                // Diana — manutenção próxima para mota 002
                new Notification
                {
                    Id = "2",
                    UserId = "user-diana-001",
                    Vin = "V-FG-2024-X1-002",
                    Title = "Revisão próxima",
                    Message = "A Fulgora X1 Sport está perto da próxima revisão aos 9000 km.",
                    Type = "maintenance",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                },
                // Diana — carregamento iniciado para mota 002
                new Notification
                {
                    Id = "3",
                    UserId = "user-diana-001",
                    Vin = "V-FG-2024-X1-002",
                    Title = "Carregamento iniciado",
                    Message = "A Fulgora X1 Sport começou a carregar.",
                    Type = "info",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                },
                // Tiago — bateria crítica para mota 003
                new Notification
                {
                    Id = "4",
                    UserId = "user-tiago-001",
                    Vin = "V-FG-2024-X1-003",
                    Title = "Bateria crítica",
                    Message = "A Fulgora X1 Eco está com apenas 5% de bateria.",
                    Type = "alert",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-45)
                },
                // Tiago — mota desconectada para mota 003
                new Notification
                {
                    Id = "5",
                    UserId = "user-tiago-001",
                    Vin = "V-FG-2024-X1-003",
                    Title = "Mota desconectada",
                    Message = "A Fulgora X1 Eco perdeu ligação ao servidor.",
                    Type = "warning",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };
        }

        public Task<List<Notification>> GetNotificationsAsync(string userId)
        {
            lock (_sync)
            {
                var result = _notifications
                    .Where(n => n.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(n => n.CreatedAt)
                    .ToList();
                return Task.FromResult(result);
            }
        }

        public Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
        {
            lock (_sync)
            {
                var result = _notifications
                    .Where(n => n.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase) && !n.IsRead)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToList();
                return Task.FromResult(result);
            }
        }

        public Task<(bool Found, bool Authorized, NotificationResult Result)> MarkAsReadAsync(string notificationId, string userId)
        {
            lock (_sync)
            {
                var notification = _notifications.FirstOrDefault(n =>
                    n.Id.Equals(notificationId, StringComparison.OrdinalIgnoreCase));

                if (notification is null)
                    return Task.FromResult((false, false,
                        new NotificationResult(false, $"Notificação '{notificationId}' não encontrada.")));

                if (!notification.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult((true, false,
                        new NotificationResult(false, $"Sem permissão para aceder à notificação '{notificationId}'.")));

                if (notification.IsRead)
                    return Task.FromResult((true, true,
                        new NotificationResult(true, $"Notificação '{notificationId}' já estava marcada como lida.")));

                notification.IsRead = true;
                return Task.FromResult((true, true,
                    new NotificationResult(true, $"Notificação '{notificationId}' marcada como lida.")));
            }
        }

        public Task<NotificationResult> CreateNotificationAsync(
            string userId, string vin, string title, string message, string type)
        {
            lock (_sync)
            {
                var notification = new Notification
                {
                    Id = (_nextId++).ToString(),
                    UserId = userId,
                    Vin = vin,
                    Title = title,
                    Message = message,
                    Type = string.IsNullOrWhiteSpace(type) ? "info" : type,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _notifications.Add(notification);
                return Task.FromResult(
                    new NotificationResult(true, $"Notificação '{title}' criada para o utilizador '{userId}'."));
            }
        }
    }
}
