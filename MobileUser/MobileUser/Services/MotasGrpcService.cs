using AMoverGRPC;
using Grpc.Core;
using MobileUser.Repositories.Interfaces;

namespace MobileUser.Services
{
    public class MotasGrpcService : MotasService.MotasServiceBase
    {
        private readonly IMotasRepository _repository;

        public MotasGrpcService(IMotasRepository repository)
        {
            _repository = repository;
        }

        public override async Task<UserDataResponse> GetUserData(UserRequest request, ServerCallContext context)
        {
            return await _repository.GetUserDataAsync();
        }

        public override async Task<MotaResponse> GetMotaInfo(MotaRequest request, ServerCallContext context)
        {
            var mota = await _repository.GetMotaInfoAsync(request.Vin);

            if (mota is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Mota não encontrada."));
            }

            return mota;
        }

        public override async Task<ActionStatus> AddGuestAccess(GuestAccessRequest request, ServerCallContext context)
        {
            return await _repository.AddGuestAccessAsync(request.Vin, request.GuestEmail);
        }

        public override async Task<ActionStatus> RemoveGuestAccess(GuestAccessRequest request, ServerCallContext context)
        {
            return await _repository.RemoveGuestAccessAsync(request.Vin, request.GuestEmail);
        }

        public override async Task<GuestListResponse> ListGuestAccess(MotaRequest request, ServerCallContext context)
        {
            return await _repository.ListGuestAccessAsync(request.Vin);
        }

        public override async Task<NotificationResponse> GetNotifications(UserRequest request, ServerCallContext context)
        {
            return await _repository.GetNotificationsAsync();
        }

        public override async Task<ActionStatus> MarkNotificationAsRead(NotificationIdRequest request, ServerCallContext context)
        {
            return await _repository.MarkNotificationAsReadAsync(request.NotificationId);
        }

        public override async Task<MaintenanceAgendaResponse> GetMaintenanceAgenda(MotaRequest request, ServerCallContext context)
        {
            return await _repository.GetMaintenanceAgendaAsync(request.Vin);
        }

        public override async Task<ActionStatus> BookMaintenanceService(BookServiceRequest request, ServerCallContext context)
        {
            return await _repository.BookMaintenanceServiceAsync(request.MaintenanceId, request.SelectedDate);
        }

        public override async Task<ActionStatus> UpdateProfilePhoto(UpdatePhotoRequest request, ServerCallContext context)
        {
            return await _repository.UpdateProfilePhotoAsync(request.ImageData.ToByteArray(), request.FileExtension);
        }

        public override async Task<ActionStatus> UpdateProfileInfo(UpdateProfileInfoRequest request, ServerCallContext context)
        {
            return await _repository.UpdateProfileInfoAsync(request.Name, request.Email);
        }
    }
}
