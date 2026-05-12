using Grpc.Core;
using TelemetryService.Grpc;
using TelemetryService.Models;
using TelemetryService.Repositories.Interfaces;
using TelemetryGrpcBase = TelemetryService.Grpc.TelemetryService;

namespace TelemetryService.Services
{
    public class TelemetryGrpcService : TelemetryGrpcBase.TelemetryServiceBase
    {
        private readonly ITelemetryRepository _repository;

        public TelemetryGrpcService(ITelemetryRepository repository)
        {
            _repository = repository;
        }

        public override async Task<TelemetryResponse> GetLatestTelemetry(
            TelemetryRequest request,
            ServerCallContext context)
        {
            var telemetry = await _repository.GetLatestTelemetryAsync(request.Vin);

            if (telemetry is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Telemetria não encontrada."));
            }

            return MapToResponse(telemetry);
        }

        public override async Task StreamTelemetry(
            TelemetryRequest request,
            IServerStreamWriter<TelemetryResponse> responseStream,
            ServerCallContext context)
        {
            while (!context.CancellationToken.IsCancellationRequested)
            {
                var telemetry = await _repository.GetLatestTelemetryAsync(request.Vin);

                if (telemetry is not null)
                {
                    await responseStream.WriteAsync(MapToResponse(telemetry));
                }

                await Task.Delay(2000, context.CancellationToken);
            }
        }

        public override async Task<ActionStatus> SaveTelemetry(
            SaveTelemetryRequest request,
            ServerCallContext context)
        {
            var telemetry = new Telemetry
            {
                Vin = request.Vin,
                Timestamp = request.Timestamp > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(request.Timestamp).UtcDateTime
                    : DateTime.UtcNow,

                BatteryLevel = request.BatteryLevel,
                BatteryTemperature = request.BatteryTemperature,
                BatteryConsumption = request.BatteryConsumption,
                BatteryRange = request.BatteryRange,

                Latitude = request.Latitude,
                Longitude = request.Longitude,

                Speed = request.Speed,
                AverageSpeed = request.AverageSpeed,
                TotalKilometers = request.TotalKilometers,

                TyreFront = request.TyreFront,
                TyreBack = request.TyreBack,

                IsConnected = request.IsConnected,
                IsStarted = request.IsStarted,
                DrivingMode = request.DrivingMode,

                Voltage = request.Voltage,
                Current = request.Current,
                Power = request.Power,

                State = request.State
            };

            await _repository.SaveTelemetryAsync(telemetry);

            return new ActionStatus
            {
                Success = true,
                Message = "Telemetria guardada com sucesso."
            };
        }

        public override async Task<TelemetryHistoryResponse> GetTelemetryHistory(
            TelemetryHistoryRequest request,
            ServerCallContext context)
        {
            var startDate = DateTimeOffset.FromUnixTimeSeconds(request.StartDate).UtcDateTime;
            var endDate = DateTimeOffset.FromUnixTimeSeconds(request.EndDate).UtcDateTime;

            var records = await _repository.GetTelemetryHistoryAsync(
                request.Vin,
                startDate,
                endDate
            );

            var response = new TelemetryHistoryResponse();

            foreach (var record in records)
            {
                response.Records.Add(MapToResponse(record));
            }

            return response;
        }

        public override async Task<VehicleConnectionStatusResponse> GetVehicleConnectionStatus(
            TelemetryRequest request,
            ServerCallContext context)
        {
            var isConnected = await _repository.IsVehicleConnectedAsync(request.Vin);
            var lastSeen = await _repository.GetLastSeenAsync(request.Vin);

            return new VehicleConnectionStatusResponse
            {
                Vin = request.Vin,
                IsConnected = isConnected,
                LastSeen = lastSeen.HasValue
                    ? new DateTimeOffset(lastSeen.Value).ToUnixTimeSeconds()
                    : 0,
                State = isConnected ? "ONLINE" : "OFFLINE"
            };
        }

        private static TelemetryResponse MapToResponse(Telemetry telemetry)
        {
            return new TelemetryResponse
            {
                Id = telemetry.Id,
                Vin = telemetry.Vin,
                Timestamp = new DateTimeOffset(telemetry.Timestamp).ToUnixTimeSeconds(),

                BatteryLevel = telemetry.BatteryLevel,
                BatteryTemperature = telemetry.BatteryTemperature,
                BatteryConsumption = telemetry.BatteryConsumption,
                BatteryRange = telemetry.BatteryRange,

                Latitude = telemetry.Latitude,
                Longitude = telemetry.Longitude,

                Speed = telemetry.Speed,
                AverageSpeed = telemetry.AverageSpeed,
                TotalKilometers = telemetry.TotalKilometers,

                TyreFront = telemetry.TyreFront,
                TyreBack = telemetry.TyreBack,

                IsConnected = telemetry.IsConnected,
                IsStarted = telemetry.IsStarted,
                DrivingMode = telemetry.DrivingMode,

                Voltage = telemetry.Voltage,
                Current = telemetry.Current,
                Power = telemetry.Power,

                State = telemetry.State
            };
        }
    }
}