using Grpc.Core;
using NotificationsService.Grpc;
using NotificationsService.Repositories.Interfaces;
using NotifSvcGrpcBase = NotificationsService.Grpc.NotificationsService;

namespace NotificationsService.Services
{
    public class NotificationsGrpcService : NotifSvcGrpcBase.NotificationsServiceBase
    {
        private readonly INotificationRepository _repository;

        public NotificationsGrpcService(INotificationRepository repository)
        {
            _repository = repository;
        }

        public override async Task<NotificationListResponse> GetNotifications(
            NotificationUserRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));

            var notifications = await _repository.GetNotificationsAsync(request.UserId);
            var response = new NotificationListResponse();
            response.Notifications.AddRange(notifications.Select(MapToProto));
            return response;
        }

        public override async Task<NotificationListResponse> GetUnreadNotifications(
            NotificationUserRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));

            var notifications = await _repository.GetUnreadNotificationsAsync(request.UserId);
            var response = new NotificationListResponse();
            response.Notifications.AddRange(notifications.Select(MapToProto));
            return response;
        }

        public override async Task<NotificationActionResponse> MarkNotificationAsRead(
            NotificationIdRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.NotificationId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "notificationId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));

            var (found, authorized, result) = await _repository.MarkAsReadAsync(request.NotificationId, request.UserId);

            if (!found)
                throw new RpcException(new Status(StatusCode.NotFound, result.Message));
            if (!authorized)
                throw new RpcException(new Status(StatusCode.PermissionDenied, result.Message));

            return new NotificationActionResponse { Success = result.Success, Message = result.Message };
        }

        public override async Task<NotificationActionResponse> CreateNotification(
            CreateNotificationRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Título é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Message))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Mensagem é obrigatória."));

            var result = await _repository.CreateNotificationAsync(
                request.UserId, request.Vin, request.Title, request.Message, request.Type);

            return new NotificationActionResponse { Success = result.Success, Message = result.Message };
        }

        public override Task<NotificationActionResponse> SendPushNotification(
            PushNotificationRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Título é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Message))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Mensagem é obrigatória."));

            // Mock: em produção integraria com serviço de push (FCM, APNs, etc.)
            return Task.FromResult(new NotificationActionResponse
            {
                Success = true,
                Message = $"Push notification mock enviada para o utilizador '{request.UserId}': {request.Title}."
            });
        }

        private static NotificationResponse MapToProto(Models.Notification n) => new()
        {
            Id = n.Id,
            UserId = n.UserId,
            Vin = n.Vin,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            IsRead = n.IsRead,
            CreatedAt = new DateTimeOffset(n.CreatedAt).ToUnixTimeSeconds()
        };
    }
}
