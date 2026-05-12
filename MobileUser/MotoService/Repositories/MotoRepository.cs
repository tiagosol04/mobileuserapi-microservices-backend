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
                return Task.FromResult(_motos.ToList());
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