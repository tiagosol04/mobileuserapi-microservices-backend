# MobileUserAPI — A-MoVeR / My Fulgora

API de microserviços para a aplicação móvel do ecossistema A-MoVeR / My Fulgora.  
Cada serviço é um processo independente em .NET 8, comunicando via gRPC (HTTP/2).

---

## Arquitectura

```
┌──────────────────┐   ┌──────────────────┐
│   MobileUser     │   │   MotoService    │
│   porta 5048     │   │   porta 5294     │
│                  │   │                  │
│ Perfil utilizador│   │ CRUD de motas    │
│ Notificações     │   │ Documentos       │
│ Manutenção       │   │ Validação de VIN │
│ Acesso convidados│   │                  │
└──────────────────┘   └──────────────────┘

┌──────────────────┐   ┌──────────────────┐
│ TelemetryService │   │  TripsService    │
│   porta 5066     │   │   porta 5278     │
│                  │   │                  │
│ Telemetria live  │   │ Início/fim viagem│
│ Histórico        │   │ Viagens recentes │
│ Streaming gRPC   │   │ Estatísticas     │
│ Estado de ligação│   │ Kms totais       │
└──────────────────┘   └──────────────────┘
```

---

## Serviços

| Serviço           | Porto HTTP | Proto               | Namespace gRPC       |
|-------------------|-----------|---------------------|----------------------|
| MobileUser        | 5048      | `mota.proto`        | `AMoverGRPC`         |
| MotoService       | 5294      | `moto.proto`        | `MotoService`        |
| TelemetryService  | 5066      | `telemetry.proto`   | `TelemetryService.Grpc` |
| TripsService      | 5278      | `trips.proto`       | `TripsService.Grpc`  |

Todos os serviços correm exclusivamente em HTTP/2 (Kestrel configurado explicitamente).  
gRPC Reflection activa apenas em `Development` (para grpcurl / Postman).

---

## Como correr

Cada serviço é um projecto independente. Abre terminais separados:

```bash
# Terminal 1
cd MobileUser/MobileUser
dotnet run

# Terminal 2
cd MobileUser/MotoService
dotnet run

# Terminal 3
cd MobileUser/TelemetryService
dotnet run

# Terminal 4
cd MobileUser/TripsService
dotnet run
```

Ou abre a solução `MobileUser/MobileUser.slnx` no Visual Studio e inicia os 4 projectos.

---

## Testar com grpcurl

Exemplos de chamadas com [grpcurl](https://github.com/fullstorydev/grpcurl) (requer serviço a correr em Development):

```bash
# Listar serviços disponíveis
grpcurl -plaintext localhost:5048 list
grpcurl -plaintext localhost:5294 list
grpcurl -plaintext localhost:5066 list
grpcurl -plaintext localhost:5278 list

# MobileUser — dados do utilizador
grpcurl -plaintext -d '{}' localhost:5048 mota.MotasService/GetUserData

# MotoService — info de uma mota por VIN
grpcurl -plaintext -d '{"vin":"V-FG-2024-X1-001"}' localhost:5294 moto.MotoService/GetMotoByVin

# TelemetryService — última telemetria
grpcurl -plaintext -d '{"vin":"V-FG-2024-X1-001"}' localhost:5066 telemetry.TelemetryService/GetLatestTelemetry

# TripsService — iniciar viagem
grpcurl -plaintext -d '{"vin":"V-FG-2024-X1-001"}' localhost:5278 trips.TripsService/StartTrip
```

---

## Dados mock

Os repositórios usam estado em memória (Singleton). Os dados reiniciam quando o processo termina.

VINs disponíveis por omissão:

| VIN                | Nome              | Estado     |
|--------------------|-------------------|------------|
| V-FG-2024-X1-001   | Fulgora X1        | Ligada     |
| V-FG-2024-X1-002   | Fulgora X1 Sport  | A carregar |
| V-FG-2024-X1-003   | Fulgora X1 Eco    | Desligada  |

---

## Estado da branch MicroServices

### Fase 1 — Concluída
- 4 microserviços independentes com gRPC e HTTP/2 explícito
- Repositórios em memória com thread-safety (`lock`)
- Validação de input nos serviços gRPC
- Streaming server-side em TelemetryService
- Validação de viagem activa antes de StartTrip
- gRPC Reflection restrita a ambiente Development

### Fase 2 — Concluída
- MobileUser transformado em BFF: agrega MotoService, TelemetryService e TripsService
- `GetMotaInfo` chama os 3 serviços downstream (MotoService obrigatório; TelemetryService e TripsService tolerantes a falha)
- `GetUserData` chama MotoService para a lista de motas e TelemetryService por cada VIN
- Validação de VIN delegada a `MotoService.ValidateMotoExists`
- Endereços dos serviços em `appsettings.json` (não hardcoded)

### Pendente (Fase 3)
- **Autenticação / JWT**: o `UserId` passado a `MotoService.ListMotosByUser` está temporariamente vazio porque ainda não existe camada de autenticação. Ver `MotasGrpcService.GetUserData` (comentário `TODO Fase 3`).
- **Autorização**: garantir que o utilizador autenticado só vê as motas associadas ao seu perfil.
- **Campos sem fonte de dados**: `is_charging`, `battery_health`, `battery_cycles`, `charging_time` não existem nos protos actuais dos serviços downstream — devolvem valores por omissão até TelemetryService ser alargado.
- **Base de dados**: substituir repositórios em memória por persistência real.
