using Grpc.Core;
using FaultsService.Repositories.Interfaces;
using FaultsSvcGrpcBase = FaultsService.Grpc.FaultsService;

namespace FaultsService.Services
{
    public class FaultsGrpcService : FaultsSvcGrpcBase.FaultsServiceBase
    {
        private readonly IFaultsRepository _repository;

        public FaultsGrpcService(IFaultsRepository repository)
        {
            _repository = repository;
        }

        public override async Task<Grpc.FaultListResponse> GetActiveFaults(
            Grpc.FaultVinRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            var faults = await _repository.GetActiveFaultsAsync(request.Vin);
            var response = new Grpc.FaultListResponse
            {
                ErrorCount = faults.Count(f => f.Severity == "ERROR"),
                WarningCount = faults.Count(f => f.Severity == "WARNING")
            };
            response.Faults.AddRange(faults.Select(MapToProto));
            return response;
        }

        public override async Task<Grpc.FaultListResponse> GetFaultsByVin(
            Grpc.FaultVinRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            var faults = await _repository.GetFaultsByVinAsync(request.Vin);
            var active = faults.Where(f => f.IsActive).ToList();
            var response = new Grpc.FaultListResponse
            {
                ErrorCount = active.Count(f => f.Severity == "ERROR"),
                WarningCount = active.Count(f => f.Severity == "WARNING")
            };
            response.Faults.AddRange(faults.Select(MapToProto));
            return response;
        }

        public override async Task<Grpc.FaultListResponse> GetWarnings(
            Grpc.FaultVinRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            var warnings = await _repository.GetWarningsAsync(request.Vin);
            var response = new Grpc.FaultListResponse
            {
                WarningCount = warnings.Count
            };
            response.Faults.AddRange(warnings.Select(MapToProto));
            return response;
        }

        public override async Task<Grpc.FaultActionResponse> RegisterFault(
            Grpc.RegisterFaultRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "título é obrigatório."));

            var result = await _repository.RegisterFaultAsync(
                request.UserId, request.Vin, request.Code,
                request.Title, request.Description, request.Severity);

            return new Grpc.FaultActionResponse
            {
                Success = result.Success,
                Message = result.Message,
                FaultId = result.FaultId
            };
        }

        public override async Task<Grpc.FaultActionResponse> ResolveFault(
            Grpc.ResolveFaultRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.FaultId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "faultId é obrigatório."));

            var result = await _repository.ResolveFaultAsync(
                request.UserId, request.Vin, request.FaultId);

            return new Grpc.FaultActionResponse
            {
                Success = result.Success,
                Message = result.Message,
                FaultId = result.FaultId
            };
        }

        public override async Task<Grpc.FaultListResponse> GetFaultHistory(
            Grpc.FaultVinRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            var faults = await _repository.GetFaultHistoryAsync(request.Vin);
            var active = faults.Where(f => f.IsActive).ToList();
            var response = new Grpc.FaultListResponse
            {
                ErrorCount = active.Count(f => f.Severity == "ERROR"),
                WarningCount = active.Count(f => f.Severity == "WARNING")
            };
            response.Faults.AddRange(faults.Select(MapToProto));
            return response;
        }

        public override async Task<Grpc.FaultActionResponse> AcknowledgeFault(
            Grpc.AcknowledgeFaultRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.FaultId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "faultId é obrigatório."));

            var result = await _repository.AcknowledgeFaultAsync(
                request.UserId, request.Vin, request.FaultId);

            return new Grpc.FaultActionResponse
            {
                Success = result.Success,
                Message = result.Message
            };
        }

        private static Grpc.FaultEntry MapToProto(Models.FaultEntry f) => new()
        {
            Id = f.Id,
            Vin = f.Vin,
            Code = f.Code,
            Title = f.Title,
            Description = f.Description,
            Severity = f.Severity,
            Timestamp = new DateTimeOffset(f.Timestamp).ToUnixTimeSeconds(),
            IsActive = f.IsActive,
            IsAcknowledged = f.IsAcknowledged
        };
    }
}
