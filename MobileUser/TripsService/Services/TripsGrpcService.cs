using Grpc.Core;
using TripsService.Grpc;
using TripsService.Models;
using TripsService.Repositories.Interfaces;
using TripsGrpcBase = TripsService.Grpc.TripsService;

namespace TripsService.Services
{
    public class TripsGrpcService : TripsGrpcBase.TripsServiceBase
    {
        private readonly ITripRepository _repository;

        public TripsGrpcService(ITripRepository repository)
        {
            _repository = repository;
        }

        public override async Task<TripResponse> StartTrip(
            TripVinRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Vin))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "VIN é obrigatório."));
            }

            var existingTrips = await _repository.GetTripsByVinAsync(request.Vin);

            if (existingTrips.Any(t => t.IsActive))
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, $"Já existe uma viagem ativa para a mota '{request.Vin}'. Termina a viagem atual antes de iniciar uma nova."));
            }

            var trip = await _repository.StartTripAsync(request.Vin);
            return MapToResponse(trip);
        }

        public override async Task<TripResponse> EndTrip(
            EndTripRequest request,
            ServerCallContext context)
        {
            var trip = await _repository.EndTripAsync(
                request.Vin,
                request.EndLatitude,
                request.EndLongitude,
                request.DistanceKm,
                request.EnergyConsumed,
                request.AverageSpeed
            );

            if (trip is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Não existe viagem ativa para esta mota."));
            }

            return MapToResponse(trip);
        }

        public override async Task<TripListResponse> GetRecentTrips(
            TripVinRequest request,
            ServerCallContext context)
        {
            var trips = await _repository.GetRecentTripsAsync(request.Vin);

            var response = new TripListResponse();

            foreach (var trip in trips)
            {
                response.Trips.Add(MapToResponse(trip));
            }

            return response;
        }

        public override async Task<TripResponse> GetTripById(
            TripIdRequest request,
            ServerCallContext context)
        {
            var trip = await _repository.GetTripByIdAsync(request.TripId);

            if (trip is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Viagem não encontrada."));
            }

            return MapToResponse(trip);
        }

        public override async Task<TripStatisticsResponse> GetTripStatistics(
            TripVinRequest request,
            ServerCallContext context)
        {
            var trips = await _repository.GetTripsByVinAsync(request.Vin);

            var completedTrips = trips.Where(t => !t.IsActive).ToList();

            var totalDistance = completedTrips.Sum(t => t.DistanceKm);
            var totalEnergy = completedTrips.Sum(t => t.EnergyConsumed);

            return new TripStatisticsResponse
            {
                Vin = request.Vin,
                TotalTrips = completedTrips.Count,
                TotalDistanceKm = totalDistance,
                TotalEnergyConsumed = totalEnergy,
                AverageSpeed = completedTrips.Any()
                    ? completedTrips.Average(t => t.AverageSpeed)
                    : 0,
                EnergyConsumptionAvg = totalDistance > 0
                    ? totalEnergy / totalDistance * 100
                    : 0
            };
        }

        public override async Task<TotalKilometersResponse> GetTotalKilometers(
            TripVinRequest request,
            ServerCallContext context)
        {
            var total = await _repository.GetTotalKilometersAsync(request.Vin);

            return new TotalKilometersResponse
            {
                Vin = request.Vin,
                TotalKilometers = total
            };
        }

        private static TripResponse MapToResponse(Trip trip)
        {
            return new TripResponse
            {
                Id = trip.Id,
                Vin = trip.Vin,
                Name = trip.Name,
                StartTime = new DateTimeOffset(trip.StartTime).ToUnixTimeSeconds(),
                EndTime = trip.EndTime.HasValue
                    ? new DateTimeOffset(trip.EndTime.Value).ToUnixTimeSeconds()
                    : 0,
                DistanceKm = trip.DistanceKm,
                EnergyConsumed = trip.EnergyConsumed,
                EnergyConsumptionAvg = trip.EnergyConsumptionAvg,
                AverageSpeed = trip.AverageSpeed,
                StartLatitude = trip.StartLatitude,
                StartLongitude = trip.StartLongitude,
                EndLatitude = trip.EndLatitude,
                EndLongitude = trip.EndLongitude,
                CreatedAt = new DateTimeOffset(trip.CreatedAt).ToUnixTimeSeconds(),
                IsActive = trip.IsActive
            };
        }
    }
}