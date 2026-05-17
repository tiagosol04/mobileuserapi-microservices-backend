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
│ Gateway / BFF    │   │ CRUD de motas    │
│ JWT Bearer auth  │   │ Documentos       │
│ Agrega respostas │   │ Validação de VIN │
│                  │   │                  │
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

┌──────────────────┐
│   UserService    │
│   porta 5182     │
│                  │
│ Perfil utilizador│
│ Atualização foto │
│ Acesso convidados│
│ UserHasAccessToVin│
└──────────────────┘
```

---

## Serviços

| Serviço           | Porto HTTP | Proto               | Namespace gRPC          |
|-------------------|-----------|---------------------|-------------------------|
| MobileUser        | 5048      | `mota.proto`        | `AMoverGRPC`            |
| MotoService       | 5294      | `moto.proto`        | `MotoService`           |
| TelemetryService  | 5066      | `telemetry.proto`   | `TelemetryService.Grpc` |
| TripsService      | 5278      | `trips.proto`       | `TripsService.Grpc`     |
| UserService       | 5182      | `user.proto`        | `UserService.Grpc`      |

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

# Terminal 5
cd MobileUser/UserService
dotnet run
```

Ou abre a solução `MobileUser/MobileUser.slnx` no Visual Studio e inicia os 5 projectos.

---

## Autenticação

O MobileUser expõe um endpoint de login mock. As credenciais são apenas para desenvolvimento — em produção seriam substituídas por um IdP externo (Keycloak, Auth0, OIDC).

### POST /auth/login

Disponível em todos os ambientes. Aceita JSON com `username` e `password`. Devolve JWT com `sub = userId`.

**Utilizadores mock disponíveis:**

| username | password | userId | Motas |
|---|---|---|---|
| `diana` | `diana123` | `user-diana-001` | V-FG-2024-X1-001, V-FG-2024-X1-002 |
| `tiago` | `tiago123` | `user-tiago-001` | V-FG-2024-X1-003 |

**Exemplos** (requer cliente HTTP/2, ex: curl com nghttp2 ou PowerShell 7):

```bash
# Login diana
curl --http2-prior-knowledge -s -X POST http://localhost:5048/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"diana","password":"diana123"}'
# Resposta: {"token":"eyJ...","userId":"user-diana-001","username":"diana"}

# Login tiago
curl --http2-prior-knowledge -s -X POST http://localhost:5048/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"tiago","password":"tiago123"}'
# Resposta: {"token":"eyJ...","userId":"user-tiago-001","username":"tiago"}

# Credenciais inválidas
curl --http2-prior-knowledge -s -X POST http://localhost:5048/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"diana","password":"errada"}'
# Resposta HTTP 401: {"error":"Credenciais inválidas."}
```

**Nota:** `/dev/token` continua disponível em Development como atalho (gera sempre token para `user-diana-001` sem credenciais). O fluxo principal de teste passa a ser `/auth/login`.

---

## Testar com grpcurl

Exemplos de chamadas com [grpcurl](https://github.com/fullstorydev/grpcurl) (requer serviço a correr em Development):

```bash
# Obter token (substituir TOKEN pelo valor devolvido pelo /auth/login)
TOKEN="eyJ..."

# Listar serviços disponíveis
grpcurl -plaintext localhost:5048 list
grpcurl -plaintext localhost:5294 list
grpcurl -plaintext localhost:5066 list
grpcurl -plaintext localhost:5278 list

# MobileUser — sem token (espera Unauthenticated)
grpcurl -plaintext -d '{}' localhost:5048 mota.MotasService/GetUserData

# MobileUser — com token diana (devolve motas 001 e 002)
grpcurl -plaintext -H "Authorization: Bearer $TOKEN" -d '{}' localhost:5048 mota.MotasService/GetUserData

# MobileUser — GetMotaInfo de mota própria (sucesso)
grpcurl -plaintext -H "Authorization: Bearer $TOKEN" \
  -d '{"vin":"V-FG-2024-X1-001"}' localhost:5048 mota.MotasService/GetMotaInfo

# MobileUser — GetMotaInfo de mota de outro utilizador (PermissionDenied)
grpcurl -plaintext -H "Authorization: Bearer $TOKEN" \
  -d '{"vin":"V-FG-2024-X1-003"}' localhost:5048 mota.MotasService/GetMotaInfo

# MotoService — info de uma mota por VIN (sem JWT — serviço interno)
grpcurl -plaintext -d '{"vin":"V-FG-2024-X1-001"}' localhost:5294 moto.MotoService/GetMotoByVin

# TelemetryService — última telemetria (sem JWT — serviço interno)
grpcurl -plaintext -d '{"vin":"V-FG-2024-X1-001"}' localhost:5066 telemetry.TelemetryService/GetLatestTelemetry

# TripsService — estatísticas de viagem (sem JWT — serviço interno)
grpcurl -plaintext -d '{"vin":"V-FG-2024-X1-001"}' localhost:5278 trips.TripsService/GetTripStatistics
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

### Fase 3A — Concluída
- JWT Bearer authentication em MobileUser (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- Todos os métodos gRPC protegidos com `[Authorize]`
- `sub` claim extraído do token e passado a `MotoService.ListMotosByUser` como `UserId`
- Autorização baseada em propriedade: `GetMotaInfo` e operações de VIN verificam se o VIN pertence ao utilizador autenticado; resposta `PermissionDenied` se não pertencer
- `GET /dev/token` disponível apenas em `Development` — gera token com `sub = "user-diana-001"` (mock)
- `MotoService`: 3 motas com `UserId` (001 e 002 → `user-diana-001`; 003 → `user-tiago-001`); `ListMotosByUserAsync` filtra por utilizador; `userId` vazio devolve lista vazia

### Fase 3B — Concluída
- Endpoint `POST /auth/login` com credenciais mock (disponível em todos os ambientes)
- Dois utilizadores mock: `diana` (motas 001 e 002) e `tiago` (mota 003)
- Login devolve JWT com `sub = userId`, `userId` e `username`
- Credenciais inválidas devolvem HTTP 401 com `{"error":"Credenciais inválidas."}`
- `/dev/token` mantido em Development como atalho (gera token para diana sem credenciais)
- Em produção, `/auth/login` seria substituído por integração com IdP externo (Keycloak, Auth0, OIDC)
- Serviços internos (MotoService, TelemetryService, TripsService) continuam sem JWT: a fronteira de segurança é o MobileUser/BFF; em produção seriam protegidos por rede privada ou mTLS

### Fase 4A — Concluída
- **UserService** criado como microserviço independente (porta 5182, `user.proto`, namespace `UserService.Grpc`)
- Perfil de utilizador, atualização de perfil e foto de perfil migrados do `MotasRepository` para `UserService`
- Gestão de guest access (AddGuestAccess, RemoveGuestAccess, ListGuestAccess) migrada para `UserService`
- `UserHasAccessToVin` implementado no `UserService` como fonte de verdade para controlo de acesso por VIN
  - Verifica ownership (userId → VINs) e guest access ativo (por email do utilizador)
  - Substitui a verificação anterior via `MotoService.ListMotosByUser`
- `MotasGrpcService` (BFF) passa a chamar `UserService` para todas as operações de utilizador e permissões
- `GetUserData` obtém perfil do `UserService.GetUserProfile` em vez de construir a partir dos claims JWT
- `MotasRepository` reduzido: mantém apenas notificações (TODO Fase 4B) e manutenção (TODO Fase 4C)
- Dados continuam mock em memória; `MobileUser` continua a ser o único ponto com JWT externo

### Pendente (Fase 4B+)
- **Fase 4B**: NotificationsService — extrair notificações do MotasRepository, adicionar campos userId/vin/type/priority
- **Fase 4C**: MaintenanceService — extrair agenda de manutenção do MotasRepository
- **Fase 4D**: ChargingService — novo serviço; preenche campos `is_charging`, `battery_cycles`, `charging_time` no MotaResponse
- **Fase 4E**: FaultsService — novo serviço para erros e avisos da mota
- **Fase 5**: Substituir repositórios em memória por base de dados real; substituir `/auth/login` mock por IdP externo
