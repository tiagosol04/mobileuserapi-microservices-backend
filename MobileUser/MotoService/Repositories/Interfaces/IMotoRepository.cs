using MotoService.Models;

namespace MotoService.Repositories.Interfaces
{
    public interface IMotoRepository
    {
        Task<Moto?> GetMotoByVinAsync(string vin);
        Task<List<Moto>> ListMotosByUserAsync(string userId);
        Task<Moto> RegisterMotoAsync(Moto moto);
        Task<Moto?> UpdateMotoAsync(Moto moto);
        Task<bool> ValidateMotoExistsAsync(string vin);
        Task<List<MotoDocumentModel>> GetMotoDocumentsAsync(string vin);
        Task<bool> AddMotoDocumentAsync(string vin, MotoDocumentModel document);
    }
}