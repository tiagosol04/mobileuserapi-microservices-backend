<div align="center">

# ⚡ MobileUserAPI

### A-MoVeR · *My Fulgora*

**Eight small services. One calm front door.**

The gRPC microservice backend that powers the *My Fulgora* mobile app —
where every request enters through a single guarded gateway and fans out
across a constellation of focused, independent services.

<br>

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![gRPC](https://img.shields.io/badge/gRPC-HTTP%2F2-244C5A?style=for-the-badge&logo=grpc&logoColor=white)
![Protobuf](https://img.shields.io/badge/Protocol_Buffers-EA4335?style=for-the-badge&logo=protobuf&logoColor=white)
![JWT](https://img.shields.io/badge/Auth-JWT_Bearer-000000?style=for-the-badge&logo=jsonwebtokens&logoColor=white)
![Pattern](https://img.shields.io/badge/Pattern-BFF-FF6F00?style=for-the-badge)

</div>

---

## 🌩️ The idea in one breath

*My Fulgora* is the companion app for Fulgora electric motorcycles. Behind it lives
**MobileUserAPI**: not one monolith, but **nine independent .NET 8 processes** that talk
to each other over **gRPC on HTTP/2**.

The trick is the front door. The mobile app never speaks to the fleet of services directly —
it speaks to exactly one of them, **MobileUser**, a *Backend-for-Frontend* (BFF) that:

- 🔐 **holds the only JWT boundary** — authentication and per-VIN authorization live here,
- 🧩 **aggregates** — a single call fans out to telemetry, trips, charging, faults… and returns one tidy answer,
- 🛟 **degrades gracefully** — if a non-critical service is down, the response still comes back, just lighter.

Everything downstream is a specialist that does *one thing* and does it in memory (for now).

---

## 🛰️ The shape of the system

```
                                 ┌───────────────────────────────┐
        Mobile app  ────JWT────▶ │        MobileUser  ·  5048     │
                                 │   Gateway / BFF · the only     │
                                 │   place JWT is verified        │
                                 │   Aggregates · authorizes VIN  │
                                 └───────────────┬───────────────┘
                                                 │  gRPC (internal, no JWT)
        ┌────────────────┬───────────────┬───────┴───────┬────────────────┬───────────────┐
        ▼                ▼               ▼               ▼                ▼               ▼
 ┌────────────┐  ┌──────────────┐ ┌─────────────┐ ┌─────────────┐ ┌──────────────┐ ┌────────────┐
 │ MotoService│  │ Telemetry    │ │ Trips       │ │ User        │ │ Notifications│ │ Charging   │
 │   5294     │  │   5066       │ │   5278      │ │   5182      │ │   5183       │ │   5185     │
 │ Units·VIN  │  │ Live + hist. │ │ Journeys    │ │ Profile     │ │ Per-user     │ │ Battery    │
 │ Documents  │  │ gRPC stream  │ │ Statistics  │ │ Guest access│ │ inbox        │ │ sessions   │
 └────────────┘  └──────────────┘ └─────────────┘ └─────────────┘ └──────────────┘ └────────────┘
                                   ┌──────────────┐ ┌─────────────┐
                                   │ Maintenance  │ │ Faults      │
                                   │   5184       │ │   5186      │
                                   │ Service book │ │ Errors +    │
                                   │ Next service │ │ warnings    │
                                   └──────────────┘ └─────────────┘

   ── HTTP/2 only (Kestrel, explicit) ──   gRPC Reflection enabled in Development only ──
```

---

## 🧭 Service catalog

| Service                  | Port | Proto                 | gRPC namespace                | Responsibility                                  |
|--------------------------|:----:|-----------------------|-------------------------------|-------------------------------------------------|
| **MobileUser** *(BFF)*   | 5048 | `mota.proto`          | `AMoverGRPC`                  | Gateway, JWT auth, VIN authorization, aggregation |
| **MotoService**          | 5294 | `moto.proto`          | `MotoService`                 | Motorcycle CRUD, documents, VIN validation      |
| **TelemetryService**     | 5066 | `telemetry.proto`     | `TelemetryService.Grpc`       | Live telemetry, history, server-side streaming  |
| **TripsService**         | 5278 | `trips.proto`         | `TripsService.Grpc`           | Trip start/end, recent trips, stats, total km   |
| **UserService**          | 5182 | `user.proto`          | `UserService.Grpc`            | Profile, photo, guest access, access-to-VIN     |
| **NotificationsService** | 5183 | `notifications.proto` | `NotificationsService.Grpc`   | Per-user inbox, mark-as-read, push (mock)       |
| **MaintenanceService**   | 5184 | `maintenance.proto`   | `MaintenanceService.Grpc`     | Maintenance agenda, booking, next service (km)  |
| **ChargingService**      | 5185 | `charging.proto`      | `ChargingService.Grpc`        | Charge state, sessions, battery cycles/health   |
| **FaultsService**        | 5186 | `faults.proto`        | `FaultsService.Grpc`          | Active faults, warnings, register/resolve       |

> All services run **exclusively over HTTP/2** (Kestrel is configured explicitly).
> **gRPC Reflection** is switched on **only in `Development`**, so tools like *grpcurl* and Postman can introspect them.

---

## 🧩 How the front door thinks

Two ideas make the BFF pattern here worth the trouble.

**Aggregation.** A single `GetMotaInfo` call is really a small orchestra: MotoService is
consulted first (it's *mandatory* — no bike, no answer), then Telemetry, Trips, Charging and
Faults are called **in parallel** and folded into one response. The app makes one request; the
backend does the running around.

**Tolerance.** Downstream services are split into *mandatory* and *best-effort*. If a best-effort
service (telemetry, trips, charging, faults) is unavailable, the BFF fills in sensible defaults
and returns anyway — the screen still loads. Only the critical path can fail the whole call.

```
GetMotaInfo(vin)
        │
        ├─▶ MotoService.GetMotoByVin ........ mandatory  (fail → fail)
        ├─▶ TelemetryService ................ best-effort (fail → defaults)
        ├─▶ TripsService .................... best-effort (fail → defaults)
        ├─▶ ChargingService ................. best-effort (fail → defaults)
        └─▶ FaultsService ................... best-effort (fail → defaults)
                    │
                    ▼
             one MotaResponse
```

The external contract — `mota.proto` — has stayed **stable** through every phase of the
migration. Services were carved out from behind it without the app ever noticing.

---

## 🚀 Running it

Each service is its own project. The simplest path is eight terminals:

```bash
cd MobileUser/MobileUser            && dotnet run   # 5048  · BFF / gateway
cd MobileUser/MotoService           && dotnet run   # 5294
cd MobileUser/TelemetryService      && dotnet run   # 5066
cd MobileUser/TripsService          && dotnet run   # 5278
cd MobileUser/UserService           && dotnet run   # 5182
cd MobileUser/NotificationsService  && dotnet run   # 5183
cd MobileUser/MaintenanceService    && dotnet run   # 5184
cd MobileUser/ChargingService       && dotnet run   # 5185
cd MobileUser/FaultsService         && dotnet run   # 5186
```

Or open **`MobileUser/MobileUser.slnx`** in Visual Studio and start all projects at once.

Service addresses live in each **`appsettings.json`** — never hard-coded.

---

## 🔐 Authentication

MobileUser exposes a **mock login** for development. In production this endpoint would be
swapped for an external identity provider (Keycloak, Auth0, any OIDC).

### `POST /auth/login`

Available in every environment. Send `username` and `password`; receive a **JWT** whose
`sub` claim is the `userId`.

**Mock users**

| Username | Password    | User ID           | Motorcycles                                   |
|----------|-------------|-------------------|-----------------------------------------------|
| `diana`  | `diana123`  | `user-diana-001`  | `V-FG-2024-X1-001`, `V-FG-2024-X1-002`        |
| `tiago`  | `tiago123`  | `user-tiago-001`  | `V-FG-2024-X1-003`                            |

> Requires an **HTTP/2** client (e.g. curl built with nghttp2, or PowerShell 7).

```bash
# Log in as diana
curl --http2-prior-knowledge -s -X POST http://localhost:5048/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"diana","password":"diana123"}'
# → {"token":"eyJ...","userId":"user-diana-001","username":"diana"}

# Wrong credentials
curl --http2-prior-knowledge -s -X POST http://localhost:5048/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"diana","password":"wrong"}'
# → HTTP 401  {"error":"Credenciais inválidas."}
```

> **Shortcut:** `GET /dev/token` is available **only in Development** and always issues a token
> for `user-diana-001` — no credentials needed. The primary test flow is `/auth/login`.

---

## 🧪 Poking at it with grpcurl

With a service running in Development, [grpcurl](https://github.com/fullstorydev/grpcurl)
makes exploration easy:

```bash
TOKEN="eyJ..."   # paste the value returned by /auth/login

# Discover what a service offers
grpcurl -plaintext localhost:5048 list

# BFF without a token → expects Unauthenticated
grpcurl -plaintext -d '{}' localhost:5048 mota.MotasService/GetUserData

# BFF with diana's token → returns her bikes (001 + 002)
grpcurl -plaintext -H "Authorization: Bearer $TOKEN" -d '{}' \
  localhost:5048 mota.MotasService/GetUserData

# Her own bike → success
grpcurl -plaintext -H "Authorization: Bearer $TOKEN" \
  -d '{"vin":"V-FG-2024-X1-001"}' localhost:5048 mota.MotasService/GetMotaInfo

# Someone else's bike → PermissionDenied
grpcurl -plaintext -H "Authorization: Bearer $TOKEN" \
  -d '{"vin":"V-FG-2024-X1-003"}' localhost:5048 mota.MotasService/GetMotaInfo

# Internal services carry no JWT — they trust the network boundary
grpcurl -plaintext -d '{"vin":"V-FG-2024-X1-001"}' localhost:5294 moto.MotoService/GetMotoByVin
grpcurl -plaintext -d '{"vin":"V-FG-2024-X1-001"}' localhost:5066 telemetry.TelemetryService/GetLatestTelemetry
grpcurl -plaintext -d '{"vin":"V-FG-2024-X1-001"}' localhost:5278 trips.TripsService/GetTripStatistics
```

---

## 🗃️ Mock data

Repositories keep **in-memory, thread-safe state** (Singleton, guarded by `lock`). Everything
resets when a process stops.

| VIN                | Name             | State        |
|--------------------|------------------|--------------|
| `V-FG-2024-X1-001` | Fulgora X1       | Connected    |
| `V-FG-2024-X1-002` | Fulgora X1 Sport | Charging     |
| `V-FG-2024-X1-003` | Fulgora X1 Eco   | Off          |

Charging snapshots: **001** idle (42 cycles) · **002** charging, active session (108 cycles) ·
**003** idle (312 cycles, battery health *Fair*).

---

## 🛡️ Security model at a glance

- **One boundary.** JWT is verified **only** at MobileUser. Every gRPC method there is `[Authorize]`d.
- **Ownership-based authorization.** The `sub` claim identifies the user; `UserService.UserHasAccessToVin`
  is the single source of truth for whether that user may touch a given VIN (via ownership *or* active guest access).
  A mismatch returns **`PermissionDenied`**.
- **Trusted interior.** Internal services (Moto, Telemetry, Trips, User, Notifications, Maintenance,
  Charging, Faults) carry no JWT. In production they'd sit behind a private network or **mTLS**.
- **Commands are mocks.** `StartChargingSession`, `EndChargingSession` and `SendPushNotification`
  record intent — they don't issue physical commands to a vehicle.

---

## 🧱 The build journey

A record of how the monolith-behind-a-gateway was carved into specialists — the external
`mota.proto` contract never breaking along the way.

| Phase | Milestone |
|:-----:|-----------|
| **1** | Four independent gRPC services on explicit HTTP/2 · in-memory repos with thread-safety · input validation · server-side telemetry streaming · active-trip guard before `StartTrip`. |
| **2** | MobileUser becomes a true **BFF** — aggregates Moto + Telemetry + Trips; VIN validation delegated to `MotoService.ValidateMotoExists`; addresses moved to `appsettings.json`. |
| **3A** | **JWT Bearer** added to MobileUser · all methods `[Authorize]`d · `sub` propagated to `ListMotosByUser` · ownership checks with `PermissionDenied`. |
| **3B** | **`/auth/login`** with mock users (*diana*, *tiago*) · JWT carries `sub`/`userId`/`username` · `/dev/token` kept as a Development shortcut. |
| **4A** | **UserService** extracted (5182) — profile, photo, guest access, and `UserHasAccessToVin` as the authoritative access check. |
| **4B** | **NotificationsService** extracted (5183) — per-user inbox, ownership-checked `MarkAsRead`, mock push. |
| **4C** | **MaintenanceService** extracted (5184) — agenda, booking, next-service km. `IMotasRepository` retired from the BFF; DealershipInfo kept locally, awaiting its own service. |
| **4D** | **ChargingService** extracted (5185) — charge state, sessions, battery cycles/health; wired into `GetUserData` and `GetMotaInfo` in parallel. |
| **4E** | **FaultsService** extracted (5186) — active faults, warnings, register / resolve / acknowledge; folded into the aggregation, tolerant to failure. |

---

## 🔮 Where it's headed — Phase 5+

- [ ] Replace in-memory repositories with a **real database**.
- [ ] Swap the mock `/auth/login` for an **external IdP** (Keycloak / Auth0 / OIDC).
- [ ] Harden the interior with **private networking or mTLS** between services.
- [ ] Give **DealershipInfo** its own dedicated service.

---

<div align="center">

**MobileUserAPI** — *A-MoVeR · My Fulgora*

Nine services, one contract, zero drama at the front door. ⚡

</div>
