using AMoverGRPC;

namespace MobileUser.Repositories.Interfaces;

public interface IDelegationsRepository
{
    Task<DelegationResponse> CreateDelegationAsync(string vin, string guestEmail);

    Task<DelegationResponse?> GetByInviteTokenAsync(string inviteToken);

    Task<ActionStatus> AcceptDelegationAsync(string inviteToken);

    Task<ActionStatus> DeclineDelegationAsync(string inviteToken);

    Task<DelegationListResponse> ListByVinAsync(string vin);
}