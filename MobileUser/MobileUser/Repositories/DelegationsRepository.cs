using AMoverGRPC;
using MobileUser.Repositories.Interfaces;

namespace MobileUser.Repositories;

public class DelegationsRepository : IDelegationsRepository
{
    private readonly List<DelegationResponse> _delegations = [];

    public Task<DelegationResponse> CreateDelegationAsync(string vin, string guestEmail)
    {
        var delegation = new DelegationResponse
        {
            Id = Guid.NewGuid().ToString(),
            Vin = vin,
            GuestEmail = guestEmail,
            Status = DelegationStatus.Pending,
            InviteToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        delegation.InviteLink = $"amover://delegation?token={delegation.InviteToken}";

        _delegations.Add(delegation);

        return Task.FromResult(delegation);
    }

    public Task<DelegationResponse?> GetByInviteTokenAsync(string inviteToken)
    {
        var delegation = _delegations.FirstOrDefault(d => d.InviteToken == inviteToken);
        return Task.FromResult(delegation);
    }

    public Task<ActionStatus> AcceptDelegationAsync(string inviteToken)
    {
        var delegation = _delegations.FirstOrDefault(d => d.InviteToken == inviteToken);

        if (delegation is null)
        {
            return Task.FromResult(new ActionStatus
            {
                Success = false,
                Message = "Delegação não encontrada."
            });
        }

        if (delegation.Status != DelegationStatus.Pending)
        {
            return Task.FromResult(new ActionStatus
            {
                Success = false,
                Message = "Delegação já foi processada."
            });
        }

        delegation.Status = DelegationStatus.Active;

        return Task.FromResult(new ActionStatus
        {
            Success = true,
            Message = "Delegação aceite com sucesso."
        });
    }

    public Task<ActionStatus> DeclineDelegationAsync(string inviteToken)
    {
        var delegation = _delegations.FirstOrDefault(d => d.InviteToken == inviteToken);

        if (delegation is null)
        {
            return Task.FromResult(new ActionStatus
            {
                Success = false,
                Message = "Delegação não encontrada."
            });
        }

        if (delegation.Status != DelegationStatus.Pending)
        {
            return Task.FromResult(new ActionStatus
            {
                Success = false,
                Message = "Delegação já foi processada."
            });
        }

        delegation.Status = DelegationStatus.Declined;

        return Task.FromResult(new ActionStatus
        {
            Success = true,
            Message = "Delegação recusada com sucesso."
        });
    }

    public Task<DelegationListResponse> ListByVinAsync(string vin)
    {
        var response = new DelegationListResponse();

        response.Delegations.AddRange(_delegations.Where(d => d.Vin == vin));

        return Task.FromResult(response);
    }
}