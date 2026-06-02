using Grpc.Core;
using MotoService.Models;
using MotoService.Repositories.Interfaces;

namespace MotoService.Services
{
    public class MotoGrpcService : MotoService.MotoServiceBase
    {
        private readonly IMotoRepository _repository;

        public MotoGrpcService(IMotoRepository repository)
        {
            _repository = repository;
        }

        public override async Task<MotoResponse> GetMotoByVin(MotoRequest request, ServerCallContext context)
        {
            var moto = await _repository.GetMotoByVinAsync(request.Vin);

            if (moto is null)
                throw new RpcException(new Status(StatusCode.NotFound, "Mota não encontrada."));

            return MapToResponse(moto);
        }

        public override async Task<MotoListResponse> ListMotosByUser(UserMotosRequest request, ServerCallContext context)
        {
            var motos = await _repository.ListMotosByUserAsync(request.UserId);

            var response = new MotoListResponse();

            foreach (var moto in motos)
            {
                response.Motos.Add(MapToResponse(moto));
            }

            return response;
        }

        public override async Task<MotoResponse> RegisterMoto(RegisterMotoRequest request, ServerCallContext context)
        {
            var moto = new Moto
            {
                Vin = request.Vin,
                MotoSN = request.MotoSn,
                Name = request.Name,
                Model = request.Model,
                Manufacturer = request.Manufacturer,
                BatteryCapacity = request.BatteryCapacity,
                ImageUri = request.ImageUri,
                Color = request.Color
            };

            var createdMoto = await _repository.RegisterMotoAsync(moto);

            return MapToResponse(createdMoto);
        }

        public override async Task<MotoResponse> UpdateMoto(UpdateMotoRequest request, ServerCallContext context)
        {
            var moto = new Moto
            {
                Vin = request.Vin,
                Name = request.Name,
                Model = request.Model,
                Manufacturer = request.Manufacturer,
                BatteryCapacity = request.BatteryCapacity,
                ImageUri = request.ImageUri,
                Color = request.Color
            };

            var updatedMoto = await _repository.UpdateMotoAsync(moto);

            if (updatedMoto is null)
                throw new RpcException(new Status(StatusCode.NotFound, "Mota não encontrada para atualizar."));

            return MapToResponse(updatedMoto);
        }

        public override async Task<ValidateMotoResponse> ValidateMotoExists(MotoRequest request, ServerCallContext context)
        {
            var exists = await _repository.ValidateMotoExistsAsync(request.Vin);

            return new ValidateMotoResponse
            {
                Exists = exists,
                Message = exists ? "Mota encontrada." : "Mota não encontrada."
            };
        }

        public override async Task<MotoDocumentsResponse> GetMotoDocuments(MotoRequest request, ServerCallContext context)
        {
            var documents = await _repository.GetMotoDocumentsAsync(request.Vin);

            var response = new MotoDocumentsResponse();

            foreach (var document in documents)
            {
                response.Documents.Add(MapToDocumentResponse(document));
            }

            return response;
        }

        public override async Task<ActionStatus> AddMotoDocument(AddMotoDocumentRequest request, ServerCallContext context)
        {
            var document = new MotoDocumentModel
            {
                Type = request.Document.Type,
                Uri = request.Document.Uri,
                UpdatedAt = DateTimeOffset.FromUnixTimeSeconds(request.Document.UpdatedAt).UtcDateTime
            };

            var success = await _repository.AddMotoDocumentAsync(request.Vin, document);

            return new ActionStatus
            {
                Success = success,
                Message = success ? "Documento adicionado com sucesso." : "Mota não encontrada."
            };
        }

        private static MotoResponse MapToResponse(Moto moto)
        {
            var response = new MotoResponse
            {
                Id = moto.Id,
                Vin = moto.Vin,
                MotoSn = moto.MotoSN,
                Name = moto.Name,
                Model = moto.Model,
                Manufacturer = moto.Manufacturer,
                BatteryCapacity = moto.BatteryCapacity,
                ImageUri = moto.ImageUri,
                Color = moto.Color,
                CreatedAt = new DateTimeOffset(moto.CreatedAt).ToUnixTimeSeconds(),
                UpdatedAt = new DateTimeOffset(moto.UpdatedAt).ToUnixTimeSeconds()
            };

            foreach (var document in moto.Documents)
            {
                response.Documents.Add(MapToDocumentResponse(document));
            }

            return response;
        }

        private static MotoDocument MapToDocumentResponse(MotoDocumentModel document)
        {
            return new MotoDocument
            {
                Type = document.Type,
                Uri = document.Uri,
                UpdatedAt = new DateTimeOffset(document.UpdatedAt).ToUnixTimeSeconds()
            };
        }
    }
}