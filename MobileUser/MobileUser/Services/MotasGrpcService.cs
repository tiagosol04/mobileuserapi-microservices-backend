using AMoverGRPC;
using Grpc.Core;
using MobileUser.Repositories.Interfaces;

namespace MobileUser.Services
{
    public class MotasGrpcService : MotasService.MotasServiceBase
    {
        private readonly IMotasRepository _repository;
        private readonly IDelegationsRepository _delegationsRepository;

        public MotasGrpcService(
            IMotasRepository repository,
            IDelegationsRepository delegationsRepository)
        {
            _repository = repository;
            _delegationsRepository = delegationsRepository;
        }

        public override async Task<UserDataResponse> GetUserData(UserRequest request, ServerCallContext context)
        {
            return await _repository.GetUserDataAsync();
        }

        public override async Task<MotaResponse> GetMotaInfo(MotaRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            }

            var mota = await _repository.GetMotaInfoAsync(request.Vin);

            if (mota is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Mota com VIN '{request.Vin}' não encontrada."));
            }

            return mota;
        }

        public override async Task<ActionStatus> AddGuestAccess(GuestAccessRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            }

            if (string.IsNullOrWhiteSpace(request.GuestEmail))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Email do convidado é obrigatório."));
            }

            return await _repository.AddGuestAccessAsync(request.Vin, request.GuestEmail);
        }

        public override async Task<ActionStatus> RemoveGuestAccess(GuestAccessRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            }

            if (string.IsNullOrWhiteSpace(request.GuestEmail))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Email do convidado é obrigatório."));
            }

            return await _repository.RemoveGuestAccessAsync(request.Vin, request.GuestEmail);
        }

        public override async Task<GuestListResponse> ListGuestAccess(MotaRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            }

            var mota = await _repository.GetMotaInfoAsync(request.Vin);
            if (mota is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Mota com VIN '{request.Vin}' não encontrada."));
            }

            return await _repository.ListGuestAccessAsync(request.Vin);
        }

        public override async Task<NotificationResponse> GetNotifications(UserRequest request, ServerCallContext context)
        {
            return await _repository.GetNotificationsAsync();
        }

        public override async Task<ActionStatus> MarkNotificationAsRead(NotificationIdRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.NotificationId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "ID da notificação é obrigatório."));
            }

            return await _repository.MarkNotificationAsReadAsync(request.NotificationId);
        }

        public override async Task<MaintenanceAgendaResponse> GetMaintenanceAgenda(MotaRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            }

            var mota = await _repository.GetMotaInfoAsync(request.Vin);
            if (mota is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Mota com VIN '{request.Vin}' não encontrada."));
            }

            return await _repository.GetMaintenanceAgendaAsync(request.Vin);
        }

        public override async Task<ActionStatus> BookMaintenanceService(BookServiceRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            }

            if (request.MaintenanceId <= 0)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "O ID da manutenção é inválido."));
            }

            if (string.IsNullOrWhiteSpace(request.SelectedDate))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Data é obrigatória."));
            }

            if (!DateOnly.TryParse(request.SelectedDate, out _))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "A data deve estar num formato válido (ex: 2026-05-10)."));
            }

            return await _repository.BookMaintenanceServiceAsync(request.Vin, request.MaintenanceId, request.SelectedDate);
        }

        public override async Task<ActionStatus> UpdateProfilePhoto(UpdatePhotoRequest request, ServerCallContext context)
        {
            if (request.ImageData == null || request.ImageData.IsEmpty)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Dados da imagem são obrigatórios."));
            }

            if (string.IsNullOrWhiteSpace(request.FileExtension))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Extensão do ficheiro é obrigatória."));
            }

            return await _repository.UpdateProfilePhotoAsync(request.ImageData.ToByteArray(), request.FileExtension);
        }

        public override async Task<ActionStatus> UpdateProfileInfo(UpdateProfileInfoRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Nome é obrigatório."));
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Email é obrigatório."));
            }

            return await _repository.UpdateProfileInfoAsync(request.Name, request.Email);
        }

        public override async Task<DelegationResponse> CreateDelegation(
    CreateDelegationRequest request,
    ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            }

            if (string.IsNullOrWhiteSpace(request.GuestEmail))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Email do convidado é obrigatório."));
            }

            var mota = await _repository.GetMotaInfoAsync(request.Vin);

            if (mota is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Mota com VIN '{request.Vin}' não encontrada."));
            }

            return await _delegationsRepository.CreateDelegationAsync(request.Vin, request.GuestEmail);
        }

        public override async Task<DelegationResponse> GetDelegationByInviteToken(
            InviteTokenRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.InviteToken))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invite token é obrigatório."));
            }

            var delegation = await _delegationsRepository.GetByInviteTokenAsync(request.InviteToken);

            if (delegation is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Delegação não encontrada."));
            }

            return delegation;
        }

        public override async Task<ActionStatus> AcceptDelegation(
            DelegationDecisionRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.InviteToken))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invite token é obrigatório."));
            }

            return await _delegationsRepository.AcceptDelegationAsync(request.InviteToken);
        }

        public override async Task<ActionStatus> DeclineDelegation(
            DelegationDecisionRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.InviteToken))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invite token é obrigatório."));
            }

            return await _delegationsRepository.DeclineDelegationAsync(request.InviteToken);
        }

        public override async Task<DelegationListResponse> ListDelegations(
            MotaRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            }

            var mota = await _repository.GetMotaInfoAsync(request.Vin);

            if (mota is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Mota com VIN '{request.Vin}' não encontrada."));
            }

            return await _delegationsRepository.ListByVinAsync(request.Vin);
        }
    }
}