using Grpc.Core;
using MaintenanceService.Repositories.Interfaces;
using MaintenSvcGrpcBase = MaintenanceService.Grpc.MaintenanceService;

namespace MaintenanceService.Services
{
    public class MaintenanceGrpcService : MaintenSvcGrpcBase.MaintenanceServiceBase
    {
        private readonly IMaintenanceRepository _repository;

        public MaintenanceGrpcService(IMaintenanceRepository repository)
        {
            _repository = repository;
        }

        public override async Task<Grpc.MaintenanceAgendaResponse> GetMaintenanceAgenda(
            Grpc.MaintenanceVinRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            var records = await _repository.GetMaintenanceAgendaAsync(request.Vin);
            var response = new Grpc.MaintenanceAgendaResponse();
            response.Maintenance.AddRange(records.Select(MapToProto));
            return response;
        }

        public override async Task<Grpc.MaintenanceActionResponse> BookMaintenanceService(
            Grpc.BookServiceRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            if (request.MaintenanceId <= 0)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "O ID da manutenção é inválido."));
            if (string.IsNullOrWhiteSpace(request.SelectedDate))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Data é obrigatória."));

            var result = await _repository.BookMaintenanceServiceAsync(
                request.Vin, request.MaintenanceId, request.SelectedDate);

            return new Grpc.MaintenanceActionResponse { Success = result.Success, Message = result.Message };
        }

        public override async Task<Grpc.NextServiceKmResponse> GetNextServiceKm(
            Grpc.MaintenanceVinRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            var km = await _repository.GetNextServiceKmAsync(request.Vin);
            return new Grpc.NextServiceKmResponse { Km = km };
        }

        private static Grpc.MaintenanceRecord MapToProto(Models.MaintenanceRecord r) => new()
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
            Status = MapStatus(r.Status)
        };

        private static Grpc.MaintenanceStatus MapStatus(Models.MaintenanceStatus status) => status switch
        {
            Models.MaintenanceStatus.DueSoon => Grpc.MaintenanceStatus.DueSoon,
            Models.MaintenanceStatus.Scheduled => Grpc.MaintenanceStatus.Scheduled,
            Models.MaintenanceStatus.Planned => Grpc.MaintenanceStatus.Planned,
            Models.MaintenanceStatus.Completed => Grpc.MaintenanceStatus.Completed,
            _ => Grpc.MaintenanceStatus.Unknown
        };
    }
}
