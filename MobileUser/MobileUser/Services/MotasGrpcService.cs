using AMoverGRPC;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using MobileUser.Repositories.Interfaces;
using MobileUser.Services.EmailService;
using MotoSvcClient = MotoService.MotoService.MotoServiceClient;
using TelemetrySvcClient = TelemetryService.Grpc.TelemetryService.TelemetryServiceClient;
using TripsSvcClient = TripsService.Grpc.TripsService.TripsServiceClient;
using UserSvcClient = UserService.Grpc.UserService.UserServiceClient;
using NotifSvcClient = NotificationsService.Grpc.NotificationsService.NotificationsServiceClient;
using MaintenSvcClient = MaintenanceService.Grpc.MaintenanceService.MaintenanceServiceClient;
using ChargingSvcClient = ChargingService.Grpc.ChargingService.ChargingServiceClient;
using FaultsSvcClient = FaultsService.Grpc.FaultsService.FaultsServiceClient;

namespace MobileUser.Services
{
    [Authorize]
    public class MotasGrpcService : MotasService.MotasServiceBase
    {
        private readonly IDelegationsRepository _delegationsRepository;
        private readonly IEmailService _emailService;
        private readonly IDealershipRepository _dealershipRepository;
        private readonly MotoSvcClient _motoClient;
        private readonly TelemetrySvcClient _telemetryClient;
        private readonly TripsSvcClient _tripsClient;
        private readonly UserSvcClient _userClient;
        private readonly NotifSvcClient _notificationsClient;
        private readonly MaintenSvcClient _maintenanceClient;
        private readonly ChargingSvcClient _chargingClient;
        private readonly FaultsSvcClient _faultsClient;

        public MotasGrpcService(
            IDelegationsRepository delegationsRepository,
            IEmailService emailService,
            IDealershipRepository dealershipRepository,
            MotoSvcClient motoClient,
            TelemetrySvcClient telemetryClient,
            TripsSvcClient tripsClient,
            UserSvcClient userClient,
            NotifSvcClient notificationsClient,
            MaintenSvcClient maintenanceClient,
            ChargingSvcClient chargingClient,
            FaultsSvcClient faultsClient)
        {
            _delegationsRepository = delegationsRepository;
            _emailService = emailService;
            _dealershipRepository = dealershipRepository;
            _motoClient = motoClient;
            _telemetryClient = telemetryClient;
            _tripsClient = tripsClient;
            _userClient = userClient;
            _notificationsClient = notificationsClient;
            _maintenanceClient = maintenanceClient;
            _chargingClient = chargingClient;
            _faultsClient = faultsClient;
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

            // Concessionário obtido do repositório local (Fase 4C — aguarda DealershipService)
            var dealership = await _dealershipRepository.GetDealershipInfoAsync();

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

                // Próxima revisão obtida do MaintenanceService (Fase 4C) — tolerante a falha
                var nextServiceKm = 0;
                try
                {
                    var kmResp = await _maintenanceClient.GetNextServiceKmAsync(
                        new MaintenanceService.Grpc.MaintenanceVinRequest { UserId = userId, Vin = moto.Vin });
                    nextServiceKm = kmResp.Km;
                }
                catch (RpcException) { }

                // Estado de carregamento obtido do ChargingService (Fase 4D) — tolerante a falha
                ChargingService.Grpc.ChargingStatusResponse? charging = null;
                try
                {
                    charging = await _chargingClient.GetChargingStatusAsync(
                        new ChargingService.Grpc.ChargingVinRequest { UserId = userId, Vin = moto.Vin });
                }
                catch (RpcException) { }

                // Faults activos obtidos do FaultsService (Fase 4E) — tolerante a falha
                FaultsService.Grpc.FaultListResponse? faultList = null;
                try
                {
                    faultList = await _faultsClient.GetActiveFaultsAsync(
                        new FaultsService.Grpc.FaultVinRequest { UserId = userId, Vin = moto.Vin });
                }
                catch (RpcException) { }

                response.Bikes.Add(BuildMotaResponse(moto, telemetry, null, nextServiceKm, charging, faultList));
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

            // Telemetria, viagens e carregamento em paralelo — todos tolerantes a falha
            var telemetryTask = TryCallAsync(() =>
                _telemetryClient.GetLatestTelemetryAsync(
                    new TelemetryService.Grpc.TelemetryRequest { Vin = request.Vin }).ResponseAsync);

            var tripsTask = TryCallAsync(() =>
                _tripsClient.GetTripStatisticsAsync(
                    new TripsService.Grpc.TripVinRequest { Vin = request.Vin }).ResponseAsync);

            var chargingTask = TryCallAsync(() =>
                _chargingClient.GetChargingStatusAsync(
                    new ChargingService.Grpc.ChargingVinRequest { UserId = userId, Vin = request.Vin }).ResponseAsync);

            var faultsTask = TryCallAsync(() =>
                _faultsClient.GetActiveFaultsAsync(
                    new FaultsService.Grpc.FaultVinRequest { UserId = userId, Vin = request.Vin }).ResponseAsync);

            await Task.WhenAll(telemetryTask, tripsTask, chargingTask, faultsTask);

            // Próxima revisão obtida do MaintenanceService (Fase 4C) — tolerante a falha
            var nextServiceKm = 0;
            try
            {
                var kmResp = await _maintenanceClient.GetNextServiceKmAsync(
                    new MaintenanceService.Grpc.MaintenanceVinRequest { UserId = userId, Vin = request.Vin });
                nextServiceKm = kmResp.Km;
            }
            catch (RpcException) { }

            return BuildMotaResponse(moto, telemetryTask.Result, tripsTask.Result, nextServiceKm, chargingTask.Result, faultsTask.Result);
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

            MaintenanceService.Grpc.MaintenanceAgendaResponse agendaResp;
            try
            {
                agendaResp = await _maintenanceClient.GetMaintenanceAgendaAsync(
                    new MaintenanceService.Grpc.MaintenanceVinRequest { UserId = userId, Vin = request.Vin });
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "MaintenanceService indisponível."));
            }

            var response = new MaintenanceAgendaResponse();
            foreach (var r in agendaResp.Maintenance)
            {
                response.Maintenance.Add(new MaintenanceRecord
                {
                    Id = r.Id,
                    Title = r.Title,
                    Subtitle = r.Subtitle,
                    Description = r.Description,
                    Date = r.Date,
                    KmTrigger = r.KmTrigger,
                    DaysRemaining = r.DaysRemaining,
                    KmRemaining = r.KmRemaining,
                    ServiceCenter = r.ServiceCenter,
                    Contact = r.Contact,
                    Address = r.Address,
                    Status = (AMoverGRPC.MaintenanceStatus)(int)r.Status
                });
            }
            return response;
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

            MaintenanceService.Grpc.MaintenanceActionResponse result;
            try
            {
                result = await _maintenanceClient.BookMaintenanceServiceAsync(
                    new MaintenanceService.Grpc.BookServiceRequest
                    {
                        UserId = userId,
                        Vin = request.Vin,
                        MaintenanceId = request.MaintenanceId,
                        SelectedDate = request.SelectedDate
                    });
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "MaintenanceService indisponível."));
            }

            return new ActionStatus { Success = result.Success, Message = result.Message };
        }

        public override async Task<FaultListResponse> GetFaults(MotaRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            var userId = ExtractUserId(context);
            await ValidateVinOwnershipAsync(request.Vin, userId);

            FaultsService.Grpc.FaultListResponse faultList;
            try
            {
                faultList = await _faultsClient.GetFaultHistoryAsync(
                    new FaultsService.Grpc.FaultVinRequest { UserId = userId, Vin = request.Vin });
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "FaultsService indisponível."));
            }

            var response = new FaultListResponse
            {
                ErrorCount = faultList.ErrorCount,
                WarningCount = faultList.WarningCount
            };
            foreach (var f in faultList.Faults)
            {
                response.Faults.Add(new FaultItem
                {
                    Id = f.Id,
                    Code = f.Code,
                    Title = f.Title,
                    Description = f.Description,
                    Severity = MapFaultSeverity(f.Severity),
                    Timestamp = f.Timestamp,
                    IsAcknowledged = f.IsAcknowledged
                });
            }
            return response;
        }

        public override async Task<ActionStatus> AcknowledgeFault(AcknowledgeFaultRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.FaultId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "faultId é obrigatório."));

            var userId = ExtractUserId(context);
            await ValidateVinOwnershipAsync(request.Vin, userId);

            FaultsService.Grpc.FaultActionResponse result;
            try
            {
                result = await _faultsClient.AcknowledgeFaultAsync(
                    new FaultsService.Grpc.AcknowledgeFaultRequest
                    {
                        UserId = userId,
                        Vin = request.Vin,
                        FaultId = request.FaultId
                    });
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "FaultsService indisponível."));
            }

            return new ActionStatus { Success = result.Success, Message = result.Message };
        }

        public override async Task<FaultListResponse> GetWarnings(MotaRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            var userId = ExtractUserId(context);
            await ValidateVinOwnershipAsync(request.Vin, userId);

            FaultsService.Grpc.FaultListResponse faultList;
            try
            {
                faultList = await _faultsClient.GetWarningsAsync(
                    new FaultsService.Grpc.FaultVinRequest { UserId = userId, Vin = request.Vin });
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "FaultsService indisponível."));
            }

            var response = new FaultListResponse
            {
                ErrorCount = faultList.ErrorCount,
                WarningCount = faultList.WarningCount
            };
            foreach (var f in faultList.Faults)
            {
                response.Faults.Add(new FaultItem
                {
                    Id = f.Id,
                    Code = f.Code,
                    Title = f.Title,
                    Description = f.Description,
                    Severity = MapFaultSeverity(f.Severity),
                    Timestamp = f.Timestamp,
                    IsAcknowledged = f.IsAcknowledged
                });
            }
            return response;
        }

        public override async Task<ActionStatus> ResolveFault(AcknowledgeFaultRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.FaultId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "faultId é obrigatório."));

            var userId = ExtractUserId(context);
            await ValidateVinOwnershipAsync(request.Vin, userId);

            FaultsService.Grpc.FaultActionResponse result;
            try
            {
                result = await _faultsClient.ResolveFaultAsync(
                    new FaultsService.Grpc.ResolveFaultRequest
                    {
                        UserId = userId,
                        Vin = request.Vin,
                        FaultId = request.FaultId
                    });
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "FaultsService indisponível."));
            }

            return new ActionStatus { Success = result.Success, Message = result.Message };
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
            int nextServiceKm,
            ChargingService.Grpc.ChargingStatusResponse? charging = null,
            FaultsService.Grpc.FaultListResponse? faultList = null)
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

                NextServiceKm = nextServiceKm,

                // Campos de carregamento preenchidos pelo ChargingService (Fase 4D)
                IsCharging = charging?.IsCharging ?? false,
                BatteryHealth = charging?.BatteryHealth ?? string.Empty,
                BatteryCycles = charging?.BatteryCycles ?? 0,
                ChargingTime = charging?.ChargingTime ?? string.Empty
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

            // Faults activos preenchidos pelo FaultsService (Fase 4E)
            if (faultList is not null)
            {
                foreach (var f in faultList.Faults)
                {
                    response.ActiveFaults.Add(new FaultItem
                    {
                        Id = f.Id,
                        Code = f.Code,
                        Title = f.Title,
                        Description = f.Description,
                        Severity = MapFaultSeverity(f.Severity),
                        Timestamp = f.Timestamp,
                        IsAcknowledged = f.IsAcknowledged
                    });
                }
            }

            return response;
        }

        private static FaultSeverity MapFaultSeverity(string severity) => severity.ToUpperInvariant() switch
        {
            "INFO" => FaultSeverity.FaultInfo,
            "WARNING" => FaultSeverity.FaultWarning,
            "ERROR" => FaultSeverity.FaultError,
            _ => FaultSeverity.Unknown
        };

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

        public override async Task<DelegationResponse> CreateDelegation(
            CreateDelegationRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            }

            if (string.IsNullOrWhiteSpace(request.GuestEmail))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Email do convidado é obrigatório."));
            }

            try
            {
                await _motoClient.GetMotoByVinAsync(
                    new MotoService.MotoRequest { Vin = request.Vin });
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                throw new RpcException(new Status(StatusCode.NotFound,
                    $"Mota com VIN '{request.Vin}' não encontrada."));
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable,
                    "MotoService indisponível."));
            }

            var delegation = await _delegationsRepository.CreateDelegationAsync(
                request.Vin,
                request.GuestEmail);

            await _emailService.SendDelegationInviteAsync(
                request.GuestEmail,
                delegation.InviteLink);

            return delegation;
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

            try
            {
                await _motoClient.GetMotoByVinAsync(
                    new MotoService.MotoRequest { Vin = request.Vin });
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                throw new RpcException(new Status(StatusCode.NotFound,
                    $"Mota com VIN '{request.Vin}' não encontrada."));
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable,
                    "MotoService indisponível."));
            }

            return await _delegationsRepository.ListByVinAsync(request.Vin);
        }
    }
}
