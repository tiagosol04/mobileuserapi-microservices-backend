using AMoverGRPC;
using MobileUser.Repositories.Interfaces;

namespace MobileUser.Repositories
{
    // Dados do concessionário em memória. Em produção substituir por DealershipService ou base de dados.
    public class DealershipRepository : IDealershipRepository
    {
        private readonly DealershipInfo _dealership = new()
        {
            Name = "Stand Exemplo",
            Phone = "912345678",
            Email = "stand@email.com",
            Address = "Rua Exemplo, Vila Real",
            AssistancePhone = "932222222"
        };

        public Task<DealershipInfo> GetDealershipInfoAsync() =>
            Task.FromResult(_dealership.Clone());
    }
}
