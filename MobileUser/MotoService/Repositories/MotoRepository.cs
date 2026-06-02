using MotoService.Models;
using MotoService.Repositories.Interfaces;

namespace MotoService.Repositories
{
    public class MotoRepository : IMotoRepository
    {
        private readonly object _sync = new();

        private readonly List<Moto> _motos = new()
        {
            new Moto
            {
                Id = 1,
                Vin = "V-FG-2024-X1-001",
                MotoSN = "SN-001",
                Name = "Fulgora X1",
                Model = "X1",
                Manufacturer = "A-MoVeR",
                BatteryCapacity = 7.5f,
                ImageUri = "https://exemplo.com/mota.png",
                Color = "Preto",
                UserId = "user-diana-001",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Documents = new List<MotoDocumentModel>
                {
                    new MotoDocumentModel
                    {
                        Type = "manual",
                        Uri = "https://exemplo.com/manual.pdf",
                        UpdatedAt = DateTime.UtcNow
                    },
                    new MotoDocumentModel
                    {
                        Type = "certificate",
                        Uri = "https://exemplo.com/certificado.pdf",
                        UpdatedAt = DateTime.UtcNow
                    }
                }
            },
            new Moto
            {
                Id = 2,
                Vin = "V-FG-2024-X1-002",
                MotoSN = "SN-002",
                Name = "Fulgora X1 Sport",
                Model = "X1 Sport",
                Manufacturer = "A-MoVeR",
                BatteryCapacity = 9.0f,
                ImageUri = "https://exemplo.com/mota-sport.png",
                Color = "Vermelho",
                UserId = "user-diana-001",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Documents = new List<MotoDocumentModel>
                {
                    new MotoDocumentModel
                    {
                        Type = "manual",
                        Uri = "https://exemplo.com/manual-sport.pdf",
                        UpdatedAt = DateTime.UtcNow
                    }
                }
            },
            new Moto
            {
                Id = 3,
                Vin = "V-FG-2024-X1-003",
                MotoSN = "SN-003",
                Name = "Fulgora X1 Eco",
                Model = "X1 Eco",
                Manufacturer = "A-MoVeR",
                BatteryCapacity = 6.0f,
                ImageUri = "https://exemplo.com/mota-eco.png",
                Color = "Verde",
                UserId = "user-tiago-001",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Documents = new List<MotoDocumentModel>
                {
                    new MotoDocumentModel
                    {
                        Type = "manual",
                        Uri = "https://exemplo.com/manual-eco.pdf",
                        UpdatedAt = DateTime.UtcNow
                    }
                }
            }
        };

        public Task<Moto?> GetMotoByVinAsync(string vin)
        {
            lock (_sync)
            {
                var moto = _motos.FirstOrDefault(m => m.Vin == vin);
                return Task.FromResult(moto);
            }
        }

        public Task<List<Moto>> ListMotosByUserAsync(string userId)
        {
            lock (_sync)
            {
                if (string.IsNullOrEmpty(userId))
                    return Task.FromResult(new List<Moto>());

                return Task.FromResult(
                    _motos.Where(m => m.UserId == userId).ToList());
            }
        }

        public Task<Moto> RegisterMotoAsync(Moto moto)
        {
            lock (_sync)
            {
                moto.Id = _motos.Count + 1;
                moto.CreatedAt = DateTime.UtcNow;
                moto.UpdatedAt = DateTime.UtcNow;

                _motos.Add(moto);

                return Task.FromResult(moto);
            }
        }

        public Task<Moto?> UpdateMotoAsync(Moto moto)
        {
            lock (_sync)
            {
                var existingMoto = _motos.FirstOrDefault(m => m.Vin == moto.Vin);

                if (existingMoto is null)
                    return Task.FromResult<Moto?>(null);

                existingMoto.Name = moto.Name;
                existingMoto.Model = moto.Model;
                existingMoto.Manufacturer = moto.Manufacturer;
                existingMoto.BatteryCapacity = moto.BatteryCapacity;
                existingMoto.ImageUri = moto.ImageUri;
                existingMoto.Color = moto.Color;
                existingMoto.UpdatedAt = DateTime.UtcNow;

                return Task.FromResult<Moto?>(existingMoto);
            }
        }

        public Task<bool> ValidateMotoExistsAsync(string vin)
        {
            lock (_sync)
            {
                var exists = _motos.Any(m => m.Vin == vin);
                return Task.FromResult(exists);
            }
        }

        public Task<List<MotoDocumentModel>> GetMotoDocumentsAsync(string vin)
        {
            lock (_sync)
            {
                var moto = _motos.FirstOrDefault(m => m.Vin == vin);
                return Task.FromResult(moto?.Documents.ToList() ?? new List<MotoDocumentModel>());
            }
        }

        public Task<bool> AddMotoDocumentAsync(string vin, MotoDocumentModel document)
        {
            lock (_sync)
            {
                var moto = _motos.FirstOrDefault(m => m.Vin == vin);

                if (moto is null)
                    return Task.FromResult(false);

                document.UpdatedAt = DateTime.UtcNow;
                moto.Documents.Add(document);
                moto.UpdatedAt = DateTime.UtcNow;

                return Task.FromResult(true);
            }
        }
    }
}