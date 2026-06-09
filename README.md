# Ghế Đầy — Multi-Vertical AI Booking SaaS

> "Ghế Đầy" (Full Seats) — an AI assistant that handles customer bookings over SMS in English
> and Vietnamese, for any seat-based service business: nail salons, restaurants, barbershops,
> spas, and beyond.

See [`PLAN.md`](./PLAN.md) for the full product plan and [`Design.md`](./Design.md) for the
design system reference.

## Repository Layout

```
/
├── backend/      ASP.NET Core Web API — Claude loop, business logic, data (Api → Application → Domain, Infrastructure → Application + Domain)
├── frontend/     Next.js 15 app — marketing site + owner dashboard
├── PLAN.md       Product plan
└── Design.md     Design system
```

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 8.0+ |
| Node.js | 20+ (22 recommended) |
| PostgreSQL | 15+ |

## Backend — `/backend`

```bash
cd backend

# Restore + build
dotnet build

# Run the API (https://localhost:5001 / http://localhost:5000)
dotnet run --project src/GhedDay.Api

# Apply EF Core migrations to a local Postgres instance
dotnet ef database update --project src/GhedDay.Infrastructure --startup-project src/GhedDay.Api
```

Configuration lives in `src/GhedDay.Api/appsettings.json` (committed defaults / placeholders)
and `appsettings.Development.json`. Real secrets must come from environment variables or a
secret store — never commit them.

Hangfire dashboard: `/hangfire`. Swagger: `/swagger`.

## Frontend — `/frontend`

```bash
cd frontend

# Install dependencies
npm install

# Dev server at http://localhost:3000
npm run dev

# Production build
npm run build
```

Copy `.env.local.example` → `.env.local` and fill in the values.

## Architecture at a Glance

```
[Customer SMS] → Twilio → POST /webhooks/twilio/sms
                              ↓ TwilioRequestValidator (before any DB touch)
                       ConversationOrchestrator
                              ↓ Claude tool-use loop (claude-sonnet-4-6)
                       Tool handlers (C#) — BusinessId/CustomerId bound server-side
                              ↓ Postgres writes
                       Twilio REST API → [Customer SMS]
                       DB write → SignalR BookingHub → [Owner Dashboard]
```

## Non-Negotiables

The project enforces six non-negotiable rules (see [`.cursor/rules/non-negotiables.mdc`](./.cursor/rules/non-negotiables.mdc)):

1. **Trust boundary** — `BusinessId`/`CustomerId` are never Claude tool parameters; bound from `ITenantContext`.
2. **Booking state machine** — guarded `UPDATE ... WHERE status = @expected`, assert `rowsAffected == 1`.
3. **Tenant scoping** — global EF query filters on `BusinessId`; raw SQL always filters by `business_id`.
4. **Idempotency** — every webhook inserts into `processed_events` first.
5. **Vertical config, not code** — new verticals extend `vertical_config` + `IVerticalConfigService`.
6. **Design tokens** — no hardcoded hex in components; use CSS custom properties / Tailwind aliases.
