using Grpc.Core;
using UserService.Grpc;
using UserService.Repositories.Interfaces;
using UserSvcGrpcBase = UserService.Grpc.UserService;

namespace UserService.Services
{
    public class UserGrpcService : UserSvcGrpcBase.UserServiceBase
    {
        private readonly IUserRepository _repository;

        public UserGrpcService(IUserRepository repository)
        {
            _repository = repository;
        }

        public override async Task<UserProfileResponse> GetUserProfile(
            UserProfileRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));

            var user = await _repository.GetUserByIdAsync(request.UserId);

            if (user is null)
                throw new RpcException(new Status(StatusCode.NotFound,
                    $"Utilizador '{request.UserId}' não encontrado."));

            return new UserProfileResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Username = user.Username,
                PhotoUri = user.PhotoUri,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = new DateTimeOffset(user.CreatedAt).ToUnixTimeSeconds()
            };
        }

        public override async Task<ActionStatus> UpdateProfileInfo(
            UpdateProfileInfoRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Nome é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Email é obrigatório."));

            var result = await _repository.UpdateProfileInfoAsync(
                request.UserId, request.Name, request.Email);

            return new ActionStatus { Success = result.Success, Message = result.Message };
        }

        public override async Task<ActionStatus> UpdateProfilePhoto(
            UpdateProfilePhotoRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (request.ImageData == null || request.ImageData.IsEmpty)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Dados da imagem são obrigatórios."));
            if (string.IsNullOrWhiteSpace(request.FileExtension))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Extensão do ficheiro é obrigatória."));

            var result = await _repository.UpdateProfilePhotoAsync(
                request.UserId, request.ImageData.ToArray(), request.FileExtension);

            return new ActionStatus { Success = result.Success, Message = result.Message };
        }

        public override async Task<ActionStatus> AddGuestAccess(
            GuestAccessRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.OwnerUserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "ownerUserId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.GuestEmail))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Email do convidado é obrigatório."));

            var result = await _repository.AddGuestAccessAsync(
                request.Vin, request.OwnerUserId, request.GuestEmail);

            return new ActionStatus { Success = result.Success, Message = result.Message };
        }

        public override async Task<ActionStatus> RemoveGuestAccess(
            GuestAccessRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.OwnerUserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "ownerUserId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.GuestEmail))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Email do convidado é obrigatório."));

            var result = await _repository.RemoveGuestAccessAsync(
                request.Vin, request.OwnerUserId, request.GuestEmail);

            return new ActionStatus { Success = result.Success, Message = result.Message };
        }

        public override async Task<GuestListResponse> ListGuestAccess(
            GuestListRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.OwnerUserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "ownerUserId é obrigatório."));

            var entries = await _repository.ListGuestAccessAsync(request.Vin, request.OwnerUserId);
            var response = new GuestListResponse();

            foreach (var entry in entries.OrderBy(e => e.GuestEmail))
            {
                response.Entries.Add(new GuestAccessEntry
                {
                    Id = entry.Id,
                    Vin = entry.Vin,
                    OwnerUserId = entry.OwnerUserId,
                    GuestEmail = entry.GuestEmail,
                    PermissionType = entry.PermissionType,
                    CreatedAt = new DateTimeOffset(entry.CreatedAt).ToUnixTimeSeconds(),
                    IsActive = entry.IsActive
                });
            }

            return response;
        }

        public override async Task<AccessCheckResponse> UserHasAccessToVin(
            AccessCheckRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "userId é obrigatório."));
            if (string.IsNullOrWhiteSpace(request.Vin))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));

            var hasAccess = await _repository.UserHasAccessToVinAsync(request.UserId, request.Vin);
            return new AccessCheckResponse { HasAccess = hasAccess };
        }
    }
}
