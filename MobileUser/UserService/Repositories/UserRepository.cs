using UserService.Models;
using UserService.Repositories.Interfaces;

namespace UserService.Repositories
{
    // Dados mock em memória. Em produção substituir por base de dados real.
    public class UserRepository : IUserRepository
    {
        private readonly object _sync = new();
        private int _nextGuestId = 10;

        private readonly List<User> _users = new()
        {
            new User
            {
                Id = "user-diana-001",
                Name = "Diana Costa",
                Email = "diana@example.com",
                Username = "diana",
                PhotoUri = "https://picsum.photos/200",
                PhoneNumber = "",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new User
            {
                Id = "user-tiago-001",
                Name = "Tiago Ribeiro",
                Email = "tiago@example.com",
                Username = "tiago",
                PhotoUri = "https://picsum.photos/200",
                PhoneNumber = "",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        };

        // Mapeamento userId → VINs que o utilizador possui (fonte de verdade de ownership).
        // Deve estar alinhado com os UserId do MotoService.
        private readonly Dictionary<string, HashSet<string>> _userVinOwnership = new(StringComparer.OrdinalIgnoreCase)
        {
            ["user-diana-001"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "V-FG-2024-X1-001",
                "V-FG-2024-X1-002"
            },
            ["user-tiago-001"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "V-FG-2024-X1-003"
            }
        };

        private readonly List<GuestAccess> _guestAccesses = new()
        {
            new GuestAccess
            {
                Id = 1,
                Vin = "V-FG-2024-X1-001",
                OwnerUserId = "user-diana-001",
                GuestEmail = "guest1@email.com",
                PermissionType = "viewer",
                CreatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new GuestAccess
            {
                Id = 2,
                Vin = "V-FG-2024-X1-001",
                OwnerUserId = "user-diana-001",
                GuestEmail = "guest2@email.com",
                PermissionType = "viewer",
                CreatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            },
            new GuestAccess
            {
                Id = 3,
                Vin = "V-FG-2024-X1-002",
                OwnerUserId = "user-diana-001",
                GuestEmail = "amigo@email.com",
                PermissionType = "viewer",
                CreatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            }
        };

        public Task<User?> GetUserByIdAsync(string userId)
        {
            lock (_sync)
            {
                var user = _users.FirstOrDefault(u =>
                    string.Equals(u.Id, userId, StringComparison.OrdinalIgnoreCase));
                return Task.FromResult(user);
            }
        }

        public Task<UserActionResult> UpdateProfileInfoAsync(string userId, string name, string email)
        {
            lock (_sync)
            {
                if (!IsValidEmail(email))
                    return Task.FromResult(new UserActionResult(false, "Email inválido."));

                var user = _users.FirstOrDefault(u =>
                    string.Equals(u.Id, userId, StringComparison.OrdinalIgnoreCase));

                if (user is null)
                    return Task.FromResult(new UserActionResult(false, $"Utilizador '{userId}' não encontrado."));

                user.Name = name.Trim();
                user.Email = email.Trim();

                return Task.FromResult(new UserActionResult(true, $"Perfil atualizado: {user.Name} / {user.Email}."));
            }
        }

        public Task<UserActionResult> UpdateProfilePhotoAsync(string userId, byte[] imageData, string fileExtension)
        {
            lock (_sync)
            {
                if (imageData == null || imageData.Length == 0)
                    return Task.FromResult(new UserActionResult(false, "Dados da imagem estão vazios."));

                var normalizedExt = NormalizeExtension(fileExtension);
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!allowedExtensions.Contains(normalizedExt))
                    return Task.FromResult(new UserActionResult(false,
                        $"Extensão '{fileExtension}' não é permitida. Usa: .jpg, .jpeg, .png, .webp."));

                var user = _users.FirstOrDefault(u =>
                    string.Equals(u.Id, userId, StringComparison.OrdinalIgnoreCase));

                if (user is null)
                    return Task.FromResult(new UserActionResult(false, $"Utilizador '{userId}' não encontrado."));

                // Mock: actualiza URI com timestamp para simular mudança
                user.PhotoUri = $"https://picsum.photos/200?updated={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                return Task.FromResult(new UserActionResult(true,
                    $"Foto de perfil atualizada ({imageData.Length} bytes, {normalizedExt})."));
            }
        }

        public Task<UserActionResult> AddGuestAccessAsync(string vin, string ownerUserId, string guestEmail)
        {
            lock (_sync)
            {
                if (!IsValidEmail(guestEmail))
                    return Task.FromResult(new UserActionResult(false, "Email inválido."));

                var alreadyExists = _guestAccesses.Any(g =>
                    g.Vin.Equals(vin, StringComparison.OrdinalIgnoreCase) &&
                    g.OwnerUserId.Equals(ownerUserId, StringComparison.OrdinalIgnoreCase) &&
                    g.GuestEmail.Equals(guestEmail, StringComparison.OrdinalIgnoreCase) &&
                    g.IsActive);

                if (alreadyExists)
                    return Task.FromResult(new UserActionResult(false,
                        $"O email '{guestEmail}' já tem acesso à mota '{vin}'."));

                _guestAccesses.Add(new GuestAccess
                {
                    Id = _nextGuestId++,
                    Vin = vin,
                    OwnerUserId = ownerUserId,
                    GuestEmail = guestEmail,
                    PermissionType = "viewer",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });

                return Task.FromResult(new UserActionResult(true,
                    $"Acesso atribuído ao email '{guestEmail}' para a mota '{vin}'."));
            }
        }

        public Task<UserActionResult> RemoveGuestAccessAsync(string vin, string ownerUserId, string guestEmail)
        {
            lock (_sync)
            {
                var entry = _guestAccesses.FirstOrDefault(g =>
                    g.Vin.Equals(vin, StringComparison.OrdinalIgnoreCase) &&
                    g.OwnerUserId.Equals(ownerUserId, StringComparison.OrdinalIgnoreCase) &&
                    g.GuestEmail.Equals(guestEmail, StringComparison.OrdinalIgnoreCase) &&
                    g.IsActive);

                if (entry is null)
                    return Task.FromResult(new UserActionResult(false,
                        $"O email '{guestEmail}' não tem acesso ativo à mota '{vin}'."));

                entry.IsActive = false;

                return Task.FromResult(new UserActionResult(true,
                    $"Acesso removido ao email '{guestEmail}' para a mota '{vin}'."));
            }
        }

        public Task<List<GuestAccess>> ListGuestAccessAsync(string vin, string ownerUserId)
        {
            lock (_sync)
            {
                var entries = _guestAccesses
                    .Where(g =>
                        g.Vin.Equals(vin, StringComparison.OrdinalIgnoreCase) &&
                        g.OwnerUserId.Equals(ownerUserId, StringComparison.OrdinalIgnoreCase) &&
                        g.IsActive)
                    .ToList();

                return Task.FromResult(entries);
            }
        }

        public Task<bool> UserHasAccessToVinAsync(string userId, string vin)
        {
            lock (_sync)
            {
                // Verifica se o utilizador é dono da mota
                if (_userVinOwnership.TryGetValue(userId, out var ownedVins) &&
                    ownedVins.Contains(vin))
                    return Task.FromResult(true);

                // Verifica se existe guest access ativo para o email do utilizador
                var user = _users.FirstOrDefault(u =>
                    string.Equals(u.Id, userId, StringComparison.OrdinalIgnoreCase));

                if (user is null)
                    return Task.FromResult(false);

                var hasGuestAccess = _guestAccesses.Any(g =>
                    g.Vin.Equals(vin, StringComparison.OrdinalIgnoreCase) &&
                    g.GuestEmail.Equals(user.Email, StringComparison.OrdinalIgnoreCase) &&
                    g.IsActive);

                return Task.FromResult(hasGuestAccess);
            }
        }

        private static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var trimmed = email.Trim();
            if (trimmed.Contains(' ')) return false;
            var atIndex = trimmed.IndexOf('@');
            return atIndex > 0 && atIndex < trimmed.Length - 1;
        }

        private static string NormalizeExtension(string ext)
        {
            var normalized = ext.Trim().ToLowerInvariant();
            return normalized.StartsWith('.') ? normalized : "." + normalized;
        }
    }
}
