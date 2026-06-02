using AMoverGRPC;

namespace MobileUser.Repositories.Interfaces
{
    public interface IDealershipRepository
    {
        Task<DealershipInfo> GetDealershipInfoAsync();
    }
}
