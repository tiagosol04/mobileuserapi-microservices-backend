using AMoverGRPC;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using MobileUser.Repositories.Interfaces;
using MotoSvcClient = MotoService.MotoService.MotoServiceClient;
using TelemetrySvcClient = TelemetryService.Grpc.TelemetryService.TelemetryServiceClient;
using TripsSvcClient = TripsService.Grpc.TripsService.TripsServiceClient;
using UserSvcClient = UserService.Grpc.UserService.UserServiceClient;
using NotifSvcClient = NotificationsService.Grpc.NotificationsService.NotificationsServiceClient;

namespace MobileUser.Services
{
    [Authorize]
    public class MotasGrpcService : MotasService.MotasServiceBase
    {
        private readonly IMotasRepository _repository;
        private readonly MotoSvcClient _motoClient;
        private readonly TelemetrySvcClient _telemetryClient;
        private readonly TripsSvcClient _tripsClient;
        private readonly UserSvcClient _userClient;
        private readonly NotifSvcClient _notificationsClient;

        public MotasGrpcService(
            IMotasRepository repository,
            MotoSvcClient motoClient,
            TelemetrySvcClient telemetryClient,
            TripsSvcClient tripsClient,
            UserSvcClient userClient,
            NotifSvcClient notificationsClient)
        {
            _repository = repository;
            _motoClient = motoClient;
            _telemetryClient = telemetryClient;
            _tripsClient = tripsClient;
            _userClient = userClient;
            _notificationsClient = notificationsClient;
        }

        public override async Task<UserDataResponse> GetUserData(UserRequest request, ServerCallContext context)
        {
            var userId = ExtractUserId(context);

            // Perfil obtido do UserService (Fase 4A)
            UserService.Grpc.UserProfileResponse userProfile;
            try
            {
                userProfile = await _userClient.GetUserProfileAsync(
                    new UserService.Grpc.UserProfileRequest { UserId = userId });
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "UserService indisponível."));
            }

            var profile = new UserProfile
            {
                Name = userProfile.Name,
                Email = userProfile.Email,
                Username = userProfile.Username,
                PhotoUri = userProfile.PhotoUri
            };

            var dealership = await _repository.GetDealershipInfoAsync();

            var response = new UserDataResponse
            {
                Profile = profile,
                Dealership = dealership
            };

            MotoService.MotoListResponse motoList;
            try
            {
                motoList = await _motoClient.ListMotosByUserAsync(
                    new MotoService.UserMotosRequest { UserId = userId });
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "MotoService indisponível."));
            }

            foreach (var moto in motoList.Motos.OrderBy(m => m.Name))
            {
                TelemetryService.Grpc.TelemetryResponse? telemetry = null;
                try
                {
                    telemetry = await _telemetryClient.GetLatestTelemetryAsync(
                        new TelemetryService.Grpc.TelemetryRequest { Vin = moto.Vin });
                }
                catch (RpcException) { }

                var nextServiceKm = await _repository.GetNextServiceKmAsync(moto.Vin);
                response.Bikes.Add(BuildMotaResponse(moto, telemetry, null, nextServiceKm));
            }

            return response;
        }

        public override async Task<MotaResponse> GetMotaInfo(MotaRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            var userId = ExtractUserId(context);

            // Validação de acesso via UserService (Fase 4A)
            await ValidateVinOwnershipAsync(request.Vin, userId);

            // Dados da mota via MotoService
            MotoService.MotoResponse moto;
            try
            {
                moto = await _motoClient.GetMotoByVinAsync(
                    new MotoService.MotoRequest { Vin = request.Vin });
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                throw new RpcException(new Status(StatusCode.NotFound,
                    $"Mota '{request.Vin}' não encontrada."));
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "MotoService indisponível."));
            }

            var telemetryTask = TryCallAsync(() =>
                _telemetryClient.GetLatestTelemetryAsync(
                    new TelemetryService.Grpc.TelemetryRequest { Vin = request.Vin }).ResponseAsync);

            var tripsTask = TryCallAsync(() =>
                _tripsClient.GetTripStatisticsAsync(
                    new TripsService.Grpc.TripVinRequest { Vin = request.Vin }).ResponseAsync);

            await Task.WhenAll(telemetryTask, tripsTask);

            var nextServiceKm = await _repository.GetNextServiceKmAsync(request.Vin);

            return BuildMotaResponse(moto, telemetryTask.Result, tripsTask.Result, nextServiceKm);
        }

        public override async Task<ActionStatus> AddGuestAccess(GuestAccessRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.GuestEmail))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Email do convidado é obrigatório."));

            var userId = ExtractUserId(context);
            await ValidateVinOwnershipAsync(request.Vin, userId);

            UserService.Grpc.ActionStatus result;
            try
            {
                result = await _userClient.AddGuestAccessAsync(new UserService.Grpc.GuestAccessRequest
                {
                    Vin = request.Vin,
                    OwnerUserId = userId,
                    GuestEmail = request.GuestEmail
                });
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "UserService indisponível."));
            }

            return new ActionStatus { Success = result.Success, Message = result.Message };
        }

        public override async Task<ActionStatus> RemoveGuestAccess(GuestAccessRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.GuestEmail))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Email do convidado é obrigatório."));

            var userId = ExtractUserId(context);
            await ValidateVinOwnershipAsync(request.Vin, userId);

            UserService.Grpc.ActionStatus result;
            try
            {
                result = await _userClient.RemoveGuestAccessAsync(new UserService.Grpc.GuestAccessRequest
                {
                    Vin = request.Vin,
                    OwnerUserId = userId,
                    GuestEmail = request.GuestEmail
                });
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "UserService indisponível."));
            }

            return new ActionStatus { Success = result.Success, Message = result.Message };
        }

        public override async Task<GuestListResponse> ListGuestAccess(MotaRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            var userId = ExtractUserId(context);
            await ValidateVinOwnershipAsync(request.Vin, userId);

            UserService.Grpc.GuestListResponse userGuestList;
            try
            {
                userGuestList = await _userClient.ListGuestAccessAsync(new UserService.Grpc.GuestListRequest
                {
                    Vin = request.Vin,
                    OwnerUserId = userId
                });
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "UserService indisponível."));
            }

            var response = new GuestListResponse();
            response.GuestEmails.AddRange(
                userGuestList.Entries
                    .Where(e => e.IsActive)
                    .Select(e => e.GuestEmail)
                    .OrderBy(e => e));

            return response;
        }

        public override async Task<NotificationResponse> GetNotifications(UserRequest request, ServerCallContext context)
        {
            var userId = ExtractUserId(context);

            NotificationsService.Grpc.NotificationListResponse notifList;
            try
            {
                notifList = await _notificationsClient.GetNotificationsAsync(
                    new NotificationsService.Grpc.NotificationUserRequest { UserId = userId });
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "NotificationsService indisponível."));
            }

            var response = new NotificationResponse();
            foreach (var n in notifList.Notifications)
            {
                response.Notifications.Add(new AppNotification
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Timestamp = n.CreatedAt,
                    IsRead = n.IsRead
                });
            }
            return response;
        }

        public override async Task<ActionStatus> MarkNotificationAsRead(NotificationIdRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.NotificationId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "ID da notificação é obrigatório."));

            var userId = ExtractUserId(context);

            NotificationsService.Grpc.NotificationActionResponse result;
            try
            {
                result = await _notificationsClient.MarkNotificationAsReadAsync(
                    new NotificationsService.Grpc.NotificationIdRequest
                    {
                        NotificationId = request.NotificationId,
                        UserId = userId
                    });
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                return new ActionStatus { Success = false, Message = ex.Status.Detail };
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
            {
                return new ActionStatus { Success = false, Message = ex.Status.Detail };
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "NotificationsService indisponível."));
            }

            return new ActionStatus { Success = result.Success, Message = result.Message };
        }

        public override async Task<MaintenanceAgendaResponse> GetMaintenanceAgenda(MotaRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            var userId = ExtractUserId(context);
            await ValidateVinOwnershipAsync(request.Vin, userId);

            // TODO Fase 4C: delegar ao MaintenanceService
            return await _repository.GetMaintenanceAgendaAsync(request.Vin);
        }

        public override async Task<ActionStatus> BookMaintenanceService(BookServiceRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            if (request.MaintenanceId <= 0)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "O ID da manutenção é inválido."));
            if (string.IsNullOrWhiteSpace(request.SelectedDate))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Data é obrigatória."));
            if (!DateOnly.TryParse(request.SelectedDate, out _))
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    "A data deve estar num formato válido (ex: 2026-05-10)."));

            var userId = ExtractUserId(context);
            await ValidateVinOwnershipAsync(request.Vin, userId);

            // TODO Fase 4C: delegar ao MaintenanceService
            return await _repository.BookMaintenanceServiceAsync(
                request.Vin, request.MaintenanceId, request.SelectedDate);
        }

        public override async Task<ActionStatus> UpdateProfilePhoto(UpdatePhotoRequest request, ServerCallContext context)
        {
            if (request.ImageData == null || request.ImageData.IsEmpty)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Dados da imagem são obrigatórios."));
            if (string.IsNullOrWhiteSpace(request.FileExtension))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Extensão do ficheiro é obrigatória."));

            var userId = ExtractUserId(context);

            UserService.Grpc.ActionStatus result;
            try
            {
                result = await _userClient.UpdateProfilePhotoAsync(new UserService.Grpc.UpdateProfilePhotoRequest
                {
                    UserId = userId,
                    ImageData = request.ImageData,
                    FileExtension = request.FileExtension
                });
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "UserService indisponível."));
            }

            return new ActionStatus { Success = result.Success, Message = result.Message };
        }

        public override async Task<ActionStatus> UpdateProfileInfo(UpdateProfileInfoRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Nome é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Email é obrigatório."));

            var userId = ExtractUserId(context);

            UserService.Grpc.ActionStatus result;
            try
            {
                result = await _userClient.UpdateProfileInfoAsync(new UserService.Grpc.UpdateProfileInfoRequest
                {
                    UserId = userId,
                    Name = request.Name,
                    Email = request.Email
                });
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "UserService indisponível."));
            }

            return new ActionStatus { Success = result.Success, Message = result.Message };
        }

        // Usa UserService como fonte de verdade para acesso por VIN (Fase 4A).
        // Substitui a verificação anterior via MotoService.ListMotosByUser.
        private async Task ValidateVinOwnershipAsync(string vin, string userId)
        {
            UserService.Grpc.AccessCheckResponse accessCheck;
            try
            {
                accessCheck = await _userClient.UserHasAccessToVinAsync(
                    new UserService.Grpc.AccessCheckRequest { UserId = userId, Vin = vin });
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "UserService indisponível."));
            }

            if (!accessCheck.HasAccess)
                throw new RpcException(new Status(StatusCode.PermissionDenied,
                    $"Sem permissão para aceder à mota '{vin}'."));
        }

        private static string ExtractUserId(ServerCallContext context)
        {
            var userId = context.GetHttpContext().User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                throw new RpcException(new Status(StatusCode.Unauthenticated, "Utilizador não autenticado."));
            return userId;
        }

        private static MotaResponse BuildMotaResponse(
            MotoService.MotoResponse moto,
            TelemetryService.Grpc.TelemetryResponse? telemetry,
            TripsService.Grpc.TripStatisticsResponse? tripStats,
            int nextServiceKm)
        {
            var response = new MotaResponse
            {
                Vin = moto.Vin,
                Name = moto.Name,

                IsConnected = telemetry?.IsConnected ?? false,
                IsStarted = telemetry?.IsStarted ?? false,
                DrivingMode = ParseDrivingMode(telemetry?.DrivingMode),

                BatteryLevel = (int)(telemetry?.BatteryLevel ?? 0),
                BatteryTemperature = telemetry?.BatteryTemperature ?? 0,
                BatteryConsumption = telemetry?.BatteryConsumption ?? 0,
                BatteryRange = (int)(telemetry?.BatteryRange ?? 0),

                AverageSpeed = (int)(telemetry?.AverageSpeed ?? 0),
                TyreFront = telemetry?.TyreFront ?? 0,
                TyreBack = telemetry?.TyreBack ?? 0,
                TotalKilometers = (int)(telemetry?.TotalKilometers ?? 0),

                Latitude = telemetry?.Latitude ?? 0,
                Longitude = telemetry?.Longitude ?? 0,

                EnergyConsumptionAvg = tripStats?.EnergyConsumptionAvg ?? 0,

                NextServiceKm = nextServiceKm
            };

            foreach (var doc in moto.Documents)
            {
                response.Documents.Add(new Document
                {
                    Type = doc.Type,
                    Uri = doc.Uri,
                    UpdatedAt = doc.UpdatedAt
                });
            }

            return response;
        }

        private static DrivingMode ParseDrivingMode(string? value) => value?.ToUpperInvariant() switch
        {
            "ECO" => DrivingMode.Eco,
            "NORMAL" => DrivingMode.Normal,
            "SPORT" => DrivingMode.Sport,
            _ => DrivingMode.Unknown
        };

        private static async Task<T?> TryCallAsync<T>(Func<Task<T>> call) where T : class
        {
            try { return await call(); }
            catch (RpcException) { return null; }
        }
    }
}
