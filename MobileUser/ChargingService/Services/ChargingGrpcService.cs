using Grpc.Core;
using ChargingService.Repositories.Interfaces;
using ChargingSvcGrpcBase = ChargingService.Grpc.ChargingService;

namespace ChargingService.Services
{
    public class ChargingGrpcService : ChargingSvcGrpcBase.ChargingServiceBase
    {
        private readonly IChargingRepository _repository;

        public ChargingGrpcService(IChargingRepository repository)
        {
            _repository = repository;
        }

        public override async Task<Grpc.ChargingStatusResponse> GetChargingStatus(
            Grpc.ChargingVinRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            var status = await _repository.GetChargingStatusAsync(request.Vin);
            return new Grpc.ChargingStatusResponse
            {
                Vin = status.Vin,
                IsCharging = status.IsCharging,
                ChargingTime = status.ChargingTime,
                BatteryHealth = status.BatteryHealth,
                BatteryCycles = status.BatteryCycles
            };
        }

        public override async Task<Grpc.BatteryCyclesResponse> GetBatteryCycles(
            Grpc.ChargingVinRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            var status = await _repository.GetChargingStatusAsync(request.Vin);
            return new Grpc.BatteryCyclesResponse { BatteryCycles = status.BatteryCycles };
        }

        public override async Task<Grpc.ChargingHistoryResponse> GetChargingHistory(
            Grpc.ChargingVinRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            var sessions = await _repository.GetChargingHistoryAsync(request.Vin);
            var response = new Grpc.ChargingHistoryResponse();
            response.Sessions.AddRange(sessions.Select(MapSessionToProto));
            return response;
        }

        public override async Task<Grpc.RemainingChargingTimeResponse> CalculateRemainingChargingTime(
            Grpc.ChargingVinRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            var remaining = await _repository.CalculateRemainingChargingTimeAsync(request.Vin);
            var estimated = remaining > 0
                ? DateTime.UtcNow.AddMinutes(remaining).ToString("HH:mm") + " UTC"
                : "";

            return new Grpc.RemainingChargingTimeResponse
            {
                RemainingMinutes = remaining,
                EstimatedCompletion = estimated
            };
        }

        public override async Task<Grpc.ChargingActionResponse> StartChargingSession(
            Grpc.StartChargingRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            // Registo mock de sessão — não envia comandos físicos à mota
            var result = await _repository.StartChargingSessionAsync(
                request.UserId, request.Vin, request.ChargerType, request.Location, request.InitialSoc);

            return new Grpc.ChargingActionResponse
            {
                Success = result.Success,
                Message = result.Message,
                SessionId = result.SessionId
            };
        }

        public override async Task<Grpc.ChargingActionResponse> EndChargingSession(
            Grpc.EndChargingRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            // Registo mock de fim de sessão — não envia comandos físicos à mota
            var result = await _repository.EndChargingSessionAsync(
                request.UserId, request.Vin, request.SessionId, request.FinalSoc);

            return new Grpc.ChargingActionResponse
            {
                Success = result.Success,
                Message = result.Message,
                SessionId = result.SessionId
            };
        }

        private static Grpc.ChargingSession MapSessionToProto(Models.ChargingSession s) => new()
        {
            Id = s.Id,
            Vin = s.Vin,
            StartTime = new DateTimeOffset(s.StartTime).ToUnixTimeSeconds(),
            EndTime = new DateTimeOffset(s.EndTime).ToUnixTimeSeconds(),
            InitialSoc = s.InitialSoc,
            FinalSoc = s.FinalSoc,
            IsCharging = s.IsCharging,
            ChargingTime = s.ChargingTime,
            BatteryCycles = s.BatteryCycles,
            EnergyChargedKwh = s.EnergyChargedKwh,
            ChargerType = s.ChargerType,
            StartLocation = s.StartLocation,
            EndLocation = s.EndLocation,
            TotalTimeMinutes = s.TotalTimeMinutes
        };
    }
}
