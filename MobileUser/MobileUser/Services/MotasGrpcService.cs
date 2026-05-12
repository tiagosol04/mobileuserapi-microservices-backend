using AMoverGRPC;
using Grpc.Core;
using MobileUser.Repositories.Interfaces;
using MotoSvcClient = MotoService.MotoService.MotoServiceClient;
using TelemetrySvcClient = TelemetryService.Grpc.TelemetryService.TelemetryServiceClient;
using TripsSvcClient = TripsService.Grpc.TripsService.TripsServiceClient;

namespace MobileUser.Services
{
    public class MotasGrpcService : MotasService.MotasServiceBase
    {
        private readonly IMotasRepository _repository;
        private readonly MotoSvcClient _motoClient;
        private readonly TelemetrySvcClient _telemetryClient;
        private readonly TripsSvcClient _tripsClient;

        public MotasGrpcService(
            IMotasRepository repository,
            MotoSvcClient motoClient,
            TelemetrySvcClient telemetryClient,
            TripsSvcClient tripsClient)
        {
            _repository = repository;
            _motoClient = motoClient;
            _telemetryClient = telemetryClient;
            _tripsClient = tripsClient;
        }

        public override async Task<UserDataResponse> GetUserData(UserRequest request, ServerCallContext context)
        {
            var profile = await _repository.GetUserProfileAsync();
            var dealership = await _repository.GetDealershipInfoAsync();

            var response = new UserDataResponse
            {
                Profile = profile,
                Dealership = dealership
            };

            MotoService.MotoListResponse motoList;
            try
            {
                // TODO Fase 3: substituir UserId vazio por identificador real do utilizador autenticado (JWT)
                motoList = await _motoClient.ListMotosByUserAsync(
                    new MotoService.UserMotosRequest { UserId = "" });
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

            MotoService.MotoResponse moto;
            try
            {
                moto = await _motoClient.GetMotoByVinAsync(
                    new MotoService.MotoRequest { Vin = request.Vin });
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
            {
                throw new RpcException(new Status(StatusCode.NotFound,
                    $"Mota com VIN '{request.Vin}' não encontrada."));
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

            await ValidateVinExistsAsync(request.Vin);

            return await _repository.AddGuestAccessAsync(request.Vin, request.GuestEmail);
        }

        public override async Task<ActionStatus> RemoveGuestAccess(GuestAccessRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            if (string.IsNullOrWhiteSpace(request.GuestEmail))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Email do convidado é obrigatório."));

            await ValidateVinExistsAsync(request.Vin);

            return await _repository.RemoveGuestAccessAsync(request.Vin, request.GuestEmail);
        }

        public override async Task<GuestListResponse> ListGuestAccess(MotaRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            await ValidateVinExistsAsync(request.Vin);

            return await _repository.ListGuestAccessAsync(request.Vin);
        }

        public override async Task<NotificationResponse> GetNotifications(UserRequest request, ServerCallContext context)
        {
            return await _repository.GetNotificationsAsync();
        }

        public override async Task<ActionStatus> MarkNotificationAsRead(NotificationIdRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.NotificationId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "ID da notificação é obrigatório."));

            return await _repository.MarkNotificationAsReadAsync(request.NotificationId);
        }

        public override async Task<MaintenanceAgendaResponse> GetMaintenanceAgenda(MotaRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            await ValidateVinExistsAsync(request.Vin);

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

            await ValidateVinExistsAsync(request.Vin);

            return await _repository.BookMaintenanceServiceAsync(
                request.Vin, request.MaintenanceId, request.SelectedDate);
        }

        public override async Task<ActionStatus> UpdateProfilePhoto(UpdatePhotoRequest request, ServerCallContext context)
        {
            if (request.ImageData == null || request.ImageData.IsEmpty)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Dados da imagem são obrigatórios."));

            if (string.IsNullOrWhiteSpace(request.FileExtension))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Extensão do ficheiro é obrigatória."));

            return await _repository.UpdateProfilePhotoAsync(request.ImageData.ToByteArray(), request.FileExtension);
        }

        public override async Task<ActionStatus> UpdateProfileInfo(UpdateProfileInfoRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Nome é obrigatório."));

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Email é obrigatório."));

            return await _repository.UpdateProfileInfoAsync(request.Name, request.Email);
        }

        private async Task ValidateVinExistsAsync(string vin)
        {
            MotoService.ValidateMotoResponse validation;
            try
            {
                validation = await _motoClient.ValidateMotoExistsAsync(
                    new MotoService.MotoRequest { Vin = vin });
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "MotoService indisponível."));
            }

            if (!validation.Exists)
                throw new RpcException(new Status(StatusCode.NotFound,
                    $"Mota com VIN '{vin}' não encontrada."));
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
