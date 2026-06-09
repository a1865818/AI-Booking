# Ghế Đầy — Product Plan
**Multi-Vertical AI Booking SaaS** · Version 2 · Updated 2026-06-10

> "Ghế Đầy" (Full Seats) — an AI assistant that handles customer bookings over SMS in English
> and Vietnamese, for any seat-based service business: nail salons, restaurants, barbershops,
> spas, and beyond.

---

## Table of Contents
1. [Confirmed Decisions](#1-confirmed-decisions)
2. [Architecture](#2-architecture)
3. [Design System](#3-design-system)
4. [Phased Milestones](#4-phased-milestones)
5. [Cursor Rules — Non-Negotiables](#5-cursor-rules--non-negotiables)
6. [Scaffold — Phase 0 Output](#6-scaffold--phase-0-output)
7. [Recommended First Implementation Phase](#7-recommended-first-implementation-phase)

---

## 1. Confirmed Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Design aesthetic | Dark-first (Spotify-inspired) | Immersive, premium, content-forward |
| Accent color | Emerald `#10b981` | Fresh, trustworthy, WCAG AA on dark |
| Font | Inter (Vietnamese subset) | Free, excellent VI glyph coverage, compact like CircularSp |
| Auth | ASP.NET Core Identity + JWT | MVP-simple, no Azure cost, full control |
| Repo structure | Monorepo (`/frontend` + `/backend`) | Simpler for small team, shared CI |
| Domain | `ghedday.com` (placeholder) | — |
| Media | Mux (video hosting) | Best DX, adaptive streaming, poster frames |
| Payments | Stripe Connect Express | Fastest onboarding for non-technical owners |
| Scheduler | Hangfire (Postgres storage) | Built-in retries + dashboard; simpler than Quartz.NET |
| Platform scope | **Multi-vertical SaaS** | Nail salon, restaurant, barbershop, spa — `business_type` is config, never code |

---

## 2. Architecture

### 2.1 System Topology

```
[Customer SMS]  →  Twilio  →  POST /webhooks/twilio/sms
                                      ↓ TwilioRequestValidator (before any DB touch)
                               ConversationOrchestrator
                                      ↓ Claude tool-use loop (claude-sonnet-4-6)
                               Tool handlers (C#) — SalonId/CustomerId bound server-side
                                      ↓ Postgres writes
                               Twilio REST API  →  [Customer SMS]

                               DB write  →  SignalR BookingHub  →  [Owner Dashboard]
                               [Owner Dashboard]  →  REST  →  .NET API  →  Postgres
```

Three surfaces in one monorepo:
- **(A) Marketing site** — Next.js `(marketing)` route group; sells the product
- **(B) Owner dashboard** — Next.js `(dashboard)` route group; runs the business
- **(C) Agent backend** — ASP.NET Core Web API; owns all business logic, the Claude loop, and data

---

### 2.2 .NET Solution Structure

Four projects. Strict dependency direction: `Api → Application → Domain`, `Infrastructure → Application + Domain`.

```
/backend/
  GhedDay.sln
  src/
    GhedDay.Api/
      Controllers/
        AuthController.cs
        BookingsController.cs
        OfferingsController.cs
        ResourcesController.cs
        SettingsController.cs
        CustomersController.cs
        BusinessesController.cs        ← super-admin: provision new business
      Webhooks/
        TwilioWebhookController.cs     ← TwilioRequestValidator FIRST, before any DB touch
        StripeWebhookController.cs     ← EventUtility.ConstructEvent FIRST
      Hubs/
        BookingHub.cs                  ← JWT-authenticated; group per business_{businessId}
      Middleware/
        TenantResolutionMiddleware.cs  ← sub claim → ITenantContext (never trust body)
        ExceptionHandlingMiddleware.cs
      Program.cs
      appsettings.json
      appsettings.Development.json
      GhedDay.Api.csproj

    GhedDay.Application/
      Bookings/
        Commands/
          CreateBookingHoldCommand.cs
          ConfirmBookingCommand.cs
          CancelBookingCommand.cs
          CompleteBookingCommand.cs
        Queries/
          GetAvailableSlotsQuery.cs    ← accepts optional party_size for restaurants
          GetBookingsByDateQuery.cs
      Conversations/
        Commands/
          ProcessInboundMessageCommand.cs
          ToggleAiCommand.cs
          SendHumanMessageCommand.cs
        Queries/
          GetConversationQuery.cs
          ListConversationsQuery.cs
      Customers/
        Commands/
          UpsertCustomerCommand.cs
          OptOutCustomerCommand.cs
      Waitlist/
        Commands/
          AddToWaitlistCommand.cs
          OfferWaitlistSlotCommand.cs
          ClaimWaitlistSlotCommand.cs
      Businesses/                      ← was Salons/
        Commands/
          CreateBusinessCommand.cs
          UpdateSettingsCommand.cs
          UpdateVerticalConfigCommand.cs
      Verticals/                       ← NEW: vertical-specific behavior behind an interface
        IVerticalConfigService.cs      ← GetResourceLabel(), RequiresDeposit(partySize?),
                                          GetAvailabilityParams(), GetClaudePersonaHint()
        VerticalConfigService.cs       ← reads Business.vertical_config + business_type
      Services/
        IAvailabilityService.cs
        IConversationOrchestrator.cs
        INotificationService.cs        ← SignalR dispatch abstraction
      DTOs/
        BookingDto.cs
        SlotDto.cs
        ConversationDto.cs
        MessageDto.cs
        ResourceDto.cs
      GhedDay.Application.csproj

    GhedDay.Domain/
      Entities/
        Business.cs                    ← was Salon.cs
        Booking.cs
        Customer.cs
        Conversation.cs
        Message.cs
        WaitlistEntry.cs
        Offering.cs                    ← was Service.cs (name-neutral across verticals)
        Resource.cs                    ← was Staff.cs (table, chair, technician, room, bay)
        ProcessedEvent.cs              ← idempotency guard
        Reminder.cs
      Enums/
        BookingStatus.cs               ← pending_deposit, confirmed, completed, cancelled, no_show
        BusinessType.cs                ← nail_salon, restaurant, barbershop, spa, beauty, other
        MessageDirection.cs
        WaitlistStatus.cs
        ConversationStatus.cs
        ResourceType.cs                ← technician, table, chair, room, bay
      ValueObjects/
        PhoneNumber.cs
        TimeSlot.cs
        VerticalConfig.cs              ← typed wrapper over vertical_config jsonb
      Events/
        BookingCreatedEvent.cs
        BookingConfirmedEvent.cs
        BookingCancelledEvent.cs
        EscalationRequestedEvent.cs
      GhedDay.Domain.csproj

    GhedDay.Infrastructure/
      Data/
        GhedDayDbContext.cs            ← EF Core; global query filters by BusinessId on every entity
        Migrations/
        Repositories/
          BookingRepository.cs         ← EF for reads; raw SQL+Dapper for advisory lock paths
          CustomerRepository.cs
          ConversationRepository.cs
          WaitlistRepository.cs
          ResourceRepository.cs
        QueryFilters/
          BusinessQueryFilterExtensions.cs
      AI/
        ClaudeHttpClient.cs            ← typed HttpClient; headers: x-api-key, anthropic-version: 2023-06-01
        ClaudeRequestBuilder.cs        ← injects IVerticalConfigService; builds system prompt +
                                          tool defs dynamically per business_type
        ClaudeConversationOrchestrator.cs  ← main loop, max 8 iterations
        ClaudeToolHandler.cs           ← dispatches tool_use → application commands
        Tools/
          CheckAvailabilityTool.cs     ← optional party_size param; delegates to IAvailabilityService
          CreateBookingHoldTool.cs     ← reads party_size if restaurant; service_id optional
          GetOfferingsTool.cs
          GetBusinessHoursTool.cs
          HandleCancellationTool.cs
          JoinWaitlistTool.cs
      Messaging/
        TwilioSmsService.cs
      Payments/
        StripeService.cs
        StripeConnectService.cs
      Jobs/
        HoldExpiryJob.cs               ← Hangfire
        ReminderJob.cs                 ← 24h + 1h SMS reminders
        WaitlistOfferTimeoutJob.cs
        NoShowJob.cs
      GhedDay.Infrastructure.csproj

  tests/
    GhedDay.Domain.Tests/
    GhedDay.Application.Tests/
    GhedDay.Api.Tests/
```

---

### 2.3 Next.js App Structure

```
/frontend/
  app/
    (marketing)/
      layout.tsx                 ← NavBar + Footer; no auth
      page.tsx                   ← Hero → SocialProof → VerticalShowcase → HowItWorks → Pricing → FAQ
      pricing/page.tsx
    (dashboard)/
      layout.tsx                 ← auth guard, SignalR provider, sidebar
      page.tsx                   ← Today: ResourceGrid + booking timeline
      conversations/
        page.tsx
        [id]/page.tsx            ← transcript + AI toggle
      bookings/page.tsx
      settings/page.tsx          ← Offerings / Resources / Hours / Deposit / AI Persona / Account
    api/
      auth/login/route.ts        ← proxies to .NET; sets httpOnly refresh cookie
      auth/refresh/route.ts
      auth/logout/route.ts
    layout.tsx                   ← Inter font, ThemeProvider
    globals.css
  components/
    ui/                          ← shadcn/ui base components
    marketing/
      NavBar.tsx
      Hero.tsx                   ← headline, Mux player, CTA
      SocialProof.tsx
      VerticalShowcase.tsx       ← NEW: tab pills (Nail · Restaurant · Barbershop) swap demo video
      FeatureSection.tsx
      HowItWorks.tsx
      Pricing.tsx
      Faq.tsx
      Footer.tsx
    dashboard/
      Sidebar.tsx
      ResourceGrid.tsx           ← was ChairGrid; label driven by vertical_config.resource_label_plural
      BookingCard.tsx
      ConversationList.tsx
      ConversationDetail.tsx
      AiToggle.tsx
      EscalationBadge.tsx
      SettingsForm.tsx
    shared/
      LanguageToggle.tsx
      RealtimeProvider.tsx
      ThemeProvider.tsx
  lib/
    api-client.ts
    signalr.ts
    auth.ts
    i18n.ts
  hooks/
    useRealtimeBookings.ts
    useConversation.ts
    useAuth.ts
  styles/
    tokens.css                   ← all CSS custom properties
  public/
    fonts/
    videos/
    images/
    og/og-default.png
  next.config.ts
  tailwind.config.ts
  tsconfig.json
  middleware.ts
  .env.local.example
```

---

### 2.4 Data Model (Postgres)

#### Core tables

```sql
-- Multi-vertical: every business is one row regardless of type
businesses (
  id            uuid PRIMARY KEY,
  name          varchar NOT NULL,
  slug          varchar UNIQUE NOT NULL,
  owner_user_id uuid REFERENCES users(id),
  twilio_number varchar UNIQUE,
  stripe_account_id varchar,
  timezone      varchar NOT NULL,
  business_type varchar NOT NULL,        -- 'nail_salon'|'restaurant'|'barbershop'|'spa'|'beauty'|'other'
  vertical_config jsonb NOT NULL,        -- vertical-specific config (see below)
  settings      jsonb,                   -- persona, deposit policy, hold_minutes, etc.
  created_at    timestamptz DEFAULT now()
)

-- Users own a business; super-admins have salon_id = null
users (
  id            uuid PRIMARY KEY,
  email         varchar UNIQUE NOT NULL,
  password_hash varchar NOT NULL,
  business_id   uuid REFERENCES businesses(id) NULLABLE,
  role          varchar NOT NULL,        -- 'owner'|'admin'|'super_admin'
  created_at    timestamptz DEFAULT now()
)

-- Resources: a technician, table, chair, room, or bay — one model fits all verticals
resources (
  id            uuid PRIMARY KEY,
  business_id   uuid REFERENCES businesses(id) NOT NULL,
  name          varchar NOT NULL,        -- "Table 4", "Amy", "Chair 1"
  resource_type varchar NOT NULL,        -- 'technician'|'table'|'chair'|'room'|'bay'
  capacity      int NOT NULL DEFAULT 1,  -- 1 for staff/chairs; N for restaurant tables
  is_active     bool NOT NULL DEFAULT true,
  sort_order    int NOT NULL DEFAULT 0
)

-- Offerings: services for salons; "Table Reservation" + specials for restaurants
offerings (
  id                uuid PRIMARY KEY,
  business_id       uuid REFERENCES businesses(id) NOT NULL,
  name              varchar NOT NULL,
  name_vi           varchar,
  duration_minutes  int NOT NULL,
  price_cents       int NOT NULL DEFAULT 0,
  is_resource_only  bool NOT NULL DEFAULT false,  -- true for "Table Reservation" (no specific service)
  is_active         bool NOT NULL DEFAULT true
)

business_hours (
  id            uuid PRIMARY KEY,
  business_id   uuid REFERENCES businesses(id) NOT NULL,
  day_of_week   int NOT NULL,            -- 0=Sun … 6=Sat
  open_time     time NOT NULL,
  close_time    time NOT NULL
)

customers (
  id            uuid PRIMARY KEY,
  business_id   uuid REFERENCES businesses(id) NOT NULL,
  phone_e164    varchar NOT NULL,
  name          varchar,
  language_pref varchar NOT NULL DEFAULT 'en',
  opted_out     bool NOT NULL DEFAULT false,
  created_at    timestamptz DEFAULT now(),
  UNIQUE (business_id, phone_e164)
)

conversations (
  id            uuid PRIMARY KEY,
  business_id   uuid REFERENCES businesses(id) NOT NULL,
  customer_id   uuid REFERENCES customers(id) NOT NULL,
  status        varchar NOT NULL,        -- 'active'|'paused'|'human_takeover'
  ai_enabled    bool NOT NULL DEFAULT true,
  created_at    timestamptz DEFAULT now(),
  updated_at    timestamptz DEFAULT now()
)

messages (
  id              uuid PRIMARY KEY,
  conversation_id uuid REFERENCES conversations(id) NOT NULL,
  direction       varchar NOT NULL,      -- 'inbound'|'outbound'
  body            text NOT NULL,
  created_at      timestamptz DEFAULT now()
)

bookings (
  id                     uuid PRIMARY KEY,
  business_id            uuid REFERENCES businesses(id) NOT NULL,
  customer_id            uuid REFERENCES customers(id) NOT NULL,
  offering_id            uuid REFERENCES offerings(id) NULLABLE,  -- null for resource-only
  resource_id            uuid REFERENCES resources(id) NULLABLE,
  start_time             timestamptz NOT NULL,
  end_time               timestamptz NOT NULL,
  party_size             int NULLABLE,                            -- restaurants only
  status                 varchar NOT NULL,  -- state machine (see below)
  stripe_payment_intent_id varchar,
  hold_expires_at        timestamptz,
  created_at             timestamptz DEFAULT now(),
  INDEX (business_id, start_time, status)  -- availability overlap query
)

waitlist_entries (
  id                uuid PRIMARY KEY,
  business_id       uuid REFERENCES businesses(id) NOT NULL,
  customer_id       uuid REFERENCES customers(id) NOT NULL,
  offering_id       uuid REFERENCES offerings(id) NULLABLE,
  preferred_date    date NOT NULL,
  party_size        int NULLABLE,
  offered_slot_time timestamptz,
  offer_expires_at  timestamptz,
  status            varchar NOT NULL,    -- 'waiting'|'offered'|'booked'|'expired'
  created_at        timestamptz DEFAULT now()
)

reminders (
  id            uuid PRIMARY KEY,
  booking_id    uuid REFERENCES bookings(id) NOT NULL,
  type          varchar NOT NULL,        -- '24h'|'1h'
  scheduled_for timestamptz NOT NULL,
  sent_at       timestamptz NULLABLE
)

-- Idempotency guard for all inbound webhooks
processed_events (
  id            varchar NOT NULL,        -- external event id (Twilio MessageSid, Stripe event id)
  source        varchar NOT NULL,        -- 'twilio'|'stripe'
  processed_at  timestamptz DEFAULT now(),
  PRIMARY KEY (id, source)
)
```

#### Booking state machine
```
pending_deposit  ──(Stripe payment_intent.succeeded)──►  confirmed
                                                              │
                                                    ──────────┴──────────────►  completed
                                                              │
                                              ◄──── cancelled / no_show
```
All transitions are guarded `UPDATE bookings SET status = @new WHERE id = @id AND status = @expected` — check `rowsAffected == 1`.

#### Availability query (all verticals — single query)
```sql
SELECT r.id, r.name, r.capacity
FROM resources r
WHERE r.business_id = @businessId
  AND r.is_active = true
  AND r.capacity >= @requiredCapacity          -- 1 for service bookings; party_size for restaurants
  AND NOT EXISTS (
    SELECT 1 FROM bookings b
    WHERE b.resource_id = r.id
      AND b.status IN ('pending_deposit','confirmed')
      AND tstzrange(b.start_time, b.end_time) && tstzrange(@start, @end)
  )
```

#### `vertical_config` examples
```jsonc
// nail_salon / barbershop / spa
{
  "resource_label": "Chair",
  "resource_label_plural": "Chairs",
  "deposit_required": true,
  "hold_minutes": 15
}

// restaurant
{
  "resource_label": "Table",
  "resource_label_plural": "Tables",
  "deposit_required": false,
  "deposit_threshold_party_size": 6,
  "deposit_per_head_cents": 1000,
  "default_duration_minutes": 90,
  "party_size_min": 1,
  "party_size_max": 20
}
```

---

### 2.5 Claude Tool-Use Loop (C#)

```
1.  Read ITenantContext → load Business (incl. vertical_config) from DB
2.  IVerticalConfigService.GetConfig(business) → VerticalConfig record
3.  ClaudeRequestBuilder:
      - System prompt prefix (cacheable): persona + vertical context hint + tool descriptions
        e.g., for restaurant: "Ask for party size before checking availability."
      - Tools array: same 6 tools for all verticals; party_size is optional param on
        check_availability and create_booking_hold
4.  POST https://api.anthropic.com/v1/messages
      headers: x-api-key, anthropic-version: 2023-06-01, content-type: application/json
      model: claude-sonnet-4-6
5.  stop_reason == "tool_use":
      foreach tool_use block:
        → Validate all params against DB (never trust Claude's time claim — re-query)
        → Bind BusinessId/CustomerId from ITenantContext (NEVER from tool args)
        → Execute C# handler → append tool_result
      loop (max 8 iterations → send fallback apology SMS if exceeded)
6.  stop_reason == "end_turn":
      → extract text content
      → Twilio outbound SMS
      → persist message to DB
      → push SignalR BookingHub event
```

**Tool definitions (what Claude sees):**
| Tool | Parameters | Notes |
|---|---|---|
| `check_availability` | `offering_id: string, date: string, party_size?: int` | `party_size` used for restaurants |
| `create_booking_hold` | `offering_id?: string, slot_iso: string, party_size?: int` | `offering_id` optional for resource-only |
| `get_offerings` | — | Returns offerings for current business |
| `get_business_hours` | — | Returns hours for current business |
| `handle_cancellation` | `booking_id: string` | |
| `join_waitlist` | `offering_id?: string, preferred_date: string, party_size?: int` | |

`BusinessId` and `CustomerId` are **never** tool parameters — always bound server-side from `ITenantContext`.

---

### 2.6 Auth Flow

```
POST /api/auth/login
  → ASP.NET Core Identity validates credentials
  → Issues JWT (15 min, HS256) + refresh token (7 days, hashed in DB)
  → Refresh token set as httpOnly Secure SameSite=Strict cookie

Next.js middleware.ts:
  → On 401 → hit /api/auth/refresh route handler → rotate tokens → retry

CORS:
  → Allow-Origin = NEXT_PUBLIC_APP_URL (env var)
  → credentials: true

SignalR BookingHub:
  → JWT bearer scheme
  → Group per business_{businessId}
```

---

### 2.7 SignalR Hub Events

All scoped to group `business_{businessId}`:
- `BookingCreated { booking }`
- `BookingStatusChanged { bookingId, newStatus }`
- `NewConversationMessage { conversationId, message }`
- `EscalationRequired { conversationId, customerName }`
- `AiToggled { conversationId, aiEnabled }`

---

### 2.8 Adding a New Vertical — Zero Code Required

To onboard a new business type (e.g., dog grooming):
1. Create `Business` row: `business_type = 'other'`, `vertical_config = { "resource_label": "Grooming Bay", ... }`
2. Create `Resource` rows (bays with `capacity = 1`)
3. Create `Offering` rows (services + durations)
4. Provision Twilio number + Stripe Connect
5. First SMS arrives → Claude reads `vertical_config` → books correctly

A business type that fits the **resource + offering + booking** model needs zero code. A genuinely novel booking model (e.g., multi-resource simultaneous booking) requires a discussion before any code is written.

---

## 3. Design System

### 3.1 Philosophy
A calm dark command center. Booking names, times, and conversation text are the content — the chrome disappears. Emerald accent appears only where action is required. Spotify's geometry (pill buttons, circle icons, heavy shadow-based depth) gives it a premium tactile feel without being precious about it.

### 3.2 Color Tokens (CSS custom properties)
```css
/* Backgrounds */
--bg-base:      #0d0d0d;   /* deepest layer */
--bg-surface:   #161616;   /* cards, sidebar, containers */
--bg-elevated:  #1e1e1e;   /* hover state surfaces, inputs */
--bg-overlay:   #242424;   /* dialogs, dropdowns */

/* Borders — shadow-based depth; borders only where structure needs it */
--border-subtle:  #1c1c1c;
--border-default: #2a2a2a;
--border-strong:  #383838;

/* Text */
--text-primary:   #f0f0f0;
--text-secondary: #888888;
--text-tertiary:  #4d4d4d;
--text-disabled:  #333333;

/* Accent — emerald; functional only, never decorative */
--accent:        #10b981;
--accent-hover:  #059669;
--accent-subtle: rgba(16, 185, 129, 0.12);
--accent-on:     #ffffff;

/* Semantic */
--color-error:   #f87171;
--color-warning: #fbbf24;
--color-info:    #60a5fa;

/* Shadows */
--shadow-sm:    rgba(0, 0, 0, 0.3) 0px 2px 6px;
--shadow-md:    rgba(0, 0, 0, 0.4) 0px 4px 12px;
--shadow-lg:    rgba(0, 0, 0, 0.5) 0px 8px 24px;
--shadow-inset: rgb(13, 13, 13) 0px 1px 0px, rgb(56, 56, 56) 0px 0px 0px 1px inset;
```

### 3.3 Type Scale
Font: **Inter** with Vietnamese subset (`&subset=latin,vietnamese`).
Weight binary: **700** (emphasis) or **400** (body). 600 used sparingly for subheadings.

| Role | Size | Weight | Line Height | Notes |
|---|---|---|---|---|
| Display | 48px | 700 | 56px | Hero headline |
| H1 | 36px | 700 | 44px | Page titles |
| H2 | 28px | 600 | 36px | Section heads |
| H3 | 22px | 600 | 28px | Feature heads |
| Body LG | 16px | 400 | 26px | Hero subtitle |
| Body | 15px | 400 | 24px | Standard copy |
| Small | 13px | 400 | 20px | Captions |
| Label | 12px | 600 | 16px | Uppercase, tracking 0.08em — status chips, nav |

### 3.4 Spacing & Radius
- **Spacing** (4px base): 4, 8, 12, 16, 20, 24, 32, 40, 48, 64, 80, 96px
- **Radius**: sm 4px (badges) · md 6px (inputs) · lg 8px (cards, dialogs) · xl 12px (panels, video) · pill 9999px (buttons, filters) · circle 50% (avatars, icon buttons)

### 3.5 Component Inventory

| Component | Key styles | States |
|---|---|---|
| Primary button | `--accent` bg, pill 9999px, weight 700, `--shadow-md` hover | hover / focus-ring / disabled / loading spinner |
| Secondary button | transparent, `--border-default` 1px, pill | hover: `--bg-elevated` |
| Ghost button | transparent, no border | hover: `--bg-elevated` |
| Input | `--bg-elevated`, `--shadow-inset`, 6px radius | focus: 2px `--accent` ring |
| Card | `--bg-surface`, 8px radius | hover: `--shadow-md` — no raw border |
| Table | `--bg-surface` header; rows alternate `--bg-base`/`--bg-surface` | row hover: `--bg-elevated` |
| Toast | `--bg-elevated` + `--shadow-lg`; emerald success, red error | slide-in from top |
| Skeleton | `--bg-elevated` + shimmer pulse | — |
| Modal | `--bg-surface`, `--shadow-lg`, backdrop `rgba(0,0,0,0.75)` | spring open/close |
| Empty state | Lucide icon `--text-tertiary`, 2-line message, optional CTA | — |
| Status badge | pill, Label type, color per status (emerald/amber/gray/red) | — |

All interactive elements: 2px solid `var(--accent)` focus ring, offset 2px.

### 3.6 Motion Language
- Micro (hover/toggle): 150ms ease-out
- Panel (drawers, modals): 250ms ease-out
- Page/card entry: Framer Motion `spring { stiffness: 300, damping: 30 }` (~400ms)
- New booking (SignalR): card slides down from top + 600ms emerald border pulse
- Scroll-reveal: `y: 20 → 0, opacity: 0 → 1`, staggered 100ms per item in feature sections
- `@media (prefers-reduced-motion: reduce)`: remove all transforms; keep opacity-only transitions

### 3.7 Marketing Site — Layout

**NavBar:** Fixed; `--bg-base/80` + backdrop-blur. Logo left, nav links center, EN/VI toggle + CTA pill right.

**Hero (100vh):**
- Left 55%: Display headline ("Your AI receptionist. Books every seat."), Body LG subtitle in `--text-secondary`, emerald CTA pill "Get started free"
- Right 45%: Mux player (12px radius, `--shadow-lg`) — real SMS→booking→dashboard flow, muted, looped, captioned
- Staggered fade-up: headline → subtitle (150ms) → CTA (300ms) → video (450ms)

**Social Proof:** 3 quote cards in a strip. Business type badge on each card (e.g., "Nail Salon", "Restaurant"). Proves multi-vertical.

**VerticalShowcase (replaces generic Feature Sections):**
Tab pills: "Nail Salon · Restaurant · Barbershop" — swap demo video, headline, and benefit copy per tab. Each vertical has a micro-video. Built as one component with vertical-keyed content objects so adding a 4th tab is one data entry, no code.

**Feature Sections (4):** Alternating left/right after the showcase.
1. "Speaks Vietnamese" — VI SMS thread micro-video
2. "See every booking, live" — dashboard ResourceGrid card appearing
3. "Deposits end no-shows" — payment link SMS + Stripe success
4. "Waitlist fills every gap" — waitlist offer SMS

**How It Works:** 3-step horizontal flow (CSS animated). 1 Send a text → 2 AI books + takes deposit → 3 You focus on guests.

**Pricing:** 2 cards: Starter (1 location) / Growth (multi-location). "Per location" language, not "per salon".

**FAQ:** 8 questions, accordion with smooth height animation.

**Footer:** 3-column. EN/VI toggle. `--bg-base`, `--border-subtle` top.

### 3.8 Dashboard — Layout

**ResourceGrid (home):** N cards, 1 per resource. Label driven by `vertical_config.resource_label_plural` (Chairs / Tables / Bays). Emerald = confirmed, amber = pending deposit, gray = free. SignalR: new booking → card slides in + emerald pulse.

**Conversations:** Left 320px list (unread dot, escalation badge) / Right transcript (SMS bubbles) + AI toggle + human takeover input.

**Settings tabs:** Offerings · Resources · Hours · Deposit · AI Persona · Stripe Connect. Resource tab label adapts to vertical_config.

### 3.9 Media Asset Checklist

| Asset | Dimensions | Format | Host | Notes |
|---|---|---|---|---|
| Hero demo video | 1920×1080 | mp4/webm | Mux | Real SMS→booking→dashboard; muted, captions, looping |
| Hero poster | 1920×1080 | jpg | Vercel | Frame 0 of hero video; loads before Mux |
| Vertical showcase: nail | 800×600 | mp4 | Mux | VI nail booking SMS thread |
| Vertical showcase: restaurant | 800×600 | mp4 | Mux | Restaurant party-size booking flow |
| Vertical showcase: barbershop | 800×600 | mp4 | Mux | Barber booking SMS thread |
| Feature: live dashboard | 1200×800 | mp4 | Mux | ResourceGrid card animate-in |
| Feature: deposit | 800×600 | mp4 | Mux | Payment link SMS + Stripe success |
| Feature: waitlist | 800×600 | mp4 | Mux | Waitlist offer SMS |
| How It Works animation | n/a | Lottie JSON | Vercel | ~5KB, 3 steps |
| OG image | 1200×630 | png | Vercel | Dark bg, logo, tagline |
| Favicon | 32×32 | ico | Vercel | |
| Apple touch icon | 180×180 | png | Vercel | |
| PWA icons | 192, 512px | png | Vercel | |

All images via `next/image` with `priority` on above-fold assets. All Mux videos via `@mux/mux-player-react` with `poster` prop. Target: Lighthouse performance ≥ 95 desktop.

---

## 4. Phased Milestones

### Phase 0 — Foundation & Scaffold
**Duration:** 3 days | **Depends on:** nothing | **Unblocks:** all phases

**Scope:** Monorepo init, .NET solution (4 projects + empty folder trees), Next.js skeleton, design tokens CSS, env examples, `.cursor/rules`. Zero feature logic — structure only.

**Done when:**
- `dotnet build` exits 0
- `npm run dev` serves at `localhost:3000`
- Design token CSS vars visible in browser devtools
- `.cursor/rules` committed with all 6 non-negotiables

**Risk:** None

---

### Phase 1 — Data Layer
**Duration:** 5 days (+1 day vs original for multi-vertical additions) | **Depends on:** Phase 0

**Scope:**
- EF Core entities: `Business`, `Resource`, `Offering`, `Booking`, `Customer`, `Conversation`, `Message`, `WaitlistEntry`, `ProcessedEvent`, `Reminder`
- `GhedDayDbContext` with global `BusinessId` query filters on every entity
- `VerticalConfig` value object; `IVerticalConfigService` + `VerticalConfigService`
- `BusinessType` enum, `ResourceType` enum
- Initial Postgres migration (all tables + indexes)
- Local dev seed: **two businesses** — 1 nail salon + 1 restaurant — proving vertical isolation from day one
- Hangfire registration + `/hangfire` dashboard

**Done when:**
- Migration applies cleanly to a local Postgres instance
- Both seeded businesses exist with correctly scoped data (no cross-tenant query possible)
- `IVerticalConfigService` returns correct resource label, deposit behavior, and Claude persona hint per business type
- Hangfire dashboard live at `/hangfire`

**Risk:** EF global query filter bypass needed for super-admin cross-tenant reads → plan `IQueryFilterDisabler` wrapper for those explicit paths.

---

### Phase 2 — SMS + Claude Loop ⚡ Critical Path
**Duration:** 8 days | **Depends on:** Phase 1

**Scope:**
- Twilio webhook: `TwilioRequestValidator` before any DB touch; `processed_events` insert before processing
- Customer/conversation upsert on inbound message
- Claude HTTP client (`ClaudeHttpClient`, `ClaudeRequestBuilder`, `ClaudeConversationOrchestrator`)
- `IVerticalConfigService` injects vertical context into system prompt
- Tools implemented: `check_availability`, `get_offerings`, `create_booking_hold`
- Availability query: resource-capacity-aware, covers both verticals
- Advisory-lock booking hold path (raw SQL + Dapper)
- Twilio outbound SMS
- STOP / HỦY opt-out handling

**Done when (nail salon):**
SMS to nail salon number → Claude responds with available slots in correct language → customer picks → `bookings` row with `pending_deposit` + `hold_expires_at` → payment link SMS sent. Duplicate Twilio delivery → 200 no-op.

**Done when (restaurant):**
SMS to restaurant number → Claude asks "How many people?" → "4" → Claude returns available tables → booking hold with `party_size = 4` → payment link sent (if party ≥ threshold) or confirmation SMS sent directly (if no deposit required).

**Risk (high):** Claude hallucinating slot times → mitigated by re-querying the exact slot in `CreateBookingHoldTool` before writing, regardless of what Claude passed in. Server-side validation is the last line of defence — not a Claude prompt instruction.

---

### Phase 3 — Stripe Deposits
**Duration:** 5 days | **Depends on:** Phase 2

**Scope:**
- Stripe Connect Express onboarding endpoint
- `PaymentIntent` creation in `CreateBookingHoldTool` (only if `IVerticalConfigService.RequiresDeposit(partySize)` returns true)
- Stripe webhook: `payment_intent.succeeded` → `confirmed`; `payment_intent.payment_failed` → cancel
- `processed_events` idempotency guard on all Stripe events

**Done when:**
Owner completes Express onboarding; customer pays via Stripe-hosted page; webhook fires; `booking.status = confirmed`; duplicate Stripe event delivery = 200 no-op.

**Risk:** Stripe webhook replay → confirmed by `processed_events` PRIMARY KEY constraint.

---

### Phase 4 — Reminders + Waitlist
**Duration:** 4 days | **Depends on:** Phase 3 | **Parallel with:** Phase 5

**Scope:**
- `HoldExpiryJob`: sweeps `pending_deposit` past `hold_expires_at` → cancel → trigger waitlist check
- `ReminderJob`: 24h + 1h SMS reminders
- `WaitlistOfferTimeoutJob`: expires unaccepted offers → offers to next entry
- `NoShowJob`: auto-marks no-show 15 min after `end_time`
- Full waitlist flow: `join_waitlist` tool → hold opens → offer sent → customer replies → advisory-lock claim → booking created

**Done when:**
Hold expires → booking cancelled → next waitlist entry receives offer SMS → replies to accept → booking created with `pending_deposit`. 24h reminder SMS fires. No-show auto-marked.

**Risk:** Waitlist claim race condition → same advisory-lock pattern as booking hold.

---

### Phase 5 — Owner Dashboard
**Duration:** 10 days | **Depends on:** Phase 3 | **Parallel with:** Phase 4

**Scope:**
- ASP.NET Core Identity auth: login/refresh/logout endpoints; JWT + httpOnly refresh cookie
- All dashboard pages: Today (ResourceGrid + timeline), Conversations, Bookings, Settings
- SignalR real-time: `RealtimeProvider`, `useRealtimeBookings`, new-booking animation
- AI toggle + human takeover per conversation
- Settings: Offerings, Resources (label from `vertical_config`), Hours, Deposit, AI Persona, Stripe Connect link
- EN/VI dashboard UI copy

**Done when:**
Owner logs in; Today page shows live bookings; new booking from SMS appears without refresh (SignalR); owner can pause AI and reply manually; ResourceGrid label shows "Chairs" for nail salon and "Tables" for restaurant; settings form saves successfully.

---

### Phase 6 — Marketing Site
**Duration:** 7 days | **Depends on:** Phase 0 | **Parallel with:** Phases 4–5

**Scope:**
- All marketing pages: Hero, SocialProof, VerticalShowcase, Feature Sections, HowItWorks, Pricing, FAQ, Footer
- `VerticalShowcase` component with tab pills (Nail · Restaurant · Barbershop) driving demo video + copy swap — adding a 4th tab is data-only
- EN/VI language toggle via `next-intl`
- Mux video integration: `@mux/mux-player-react` with poster frames
- Framer Motion scroll-reveal throughout
- OG/meta tags, favicon set
- Copy uses "per location", "every seat" — never "salon-only" language

**Done when:**
Lighthouse performance ≥ 95 desktop; EN/VI toggle switches all copy; VerticalShowcase tab switch works; hero video plays with poster frame; mobile layout correct at 375px viewport.

---

### Phase 7 — Hardening & Pilot
**Duration:** 5 days | **Depends on:** Phase 5 + 6

**Scope:**
- Multi-tenant isolation audit: zero EF queries without `BusinessId` global filter; zero raw SQL without `WHERE business_id = @businessId`
- Security headers: CSP, HSTS, `X-Frame-Options`, `X-Content-Type-Options`
- Azure Key Vault (managed identity) — no secrets in `appsettings.json` in staging
- Staging deploy: Azure App Service (backend) + Vercel (frontend)
- SMS round-trip load test (nail salon + restaurant both)
- WCAG AA contrast audit
- Privacy: PII minimisation review; AI processing disclosure in first SMS

**Done when:**
Audit finds zero unscoped queries; all webhook signatures verified; staging pilot SMS works end-to-end for both verticals; no secrets in config files in staging.

---

### Growth (Post-Pilot, not estimated)
- Instagram / WhatsApp channel (Meta Webhooks)
- Missed-call → SMS trigger
- Voice (Vietnamese TTS/STR)
- Multi-location dashboard (owner sees all locations in one view)
- Additional verticals (spa, dog grooming, golf bay) — should require zero code

---

### Total effort estimate
| Phase | Days | Notes |
|---|---|---|
| 0 Scaffold | 3 | |
| 1 Data layer | 5 | |
| 2 SMS + Claude | 8 | Critical path |
| 3 Stripe | 5 | |
| 4 Reminders + Waitlist | 4 | Parallel with 5 |
| 5 Dashboard | 10 | |
| 6 Marketing | 7 | Parallel with 4–5 |
| 7 Hardening | 5 | |
| **MVP total** | **~38 dev-days** | ~8 weeks solo / ~5 weeks 2-person |

---

## 5. Cursor Rules — Non-Negotiables

These apply to every session, every PR, every file touched.

### 5.1 Trust Boundary
- `BusinessId` and `CustomerId` **must never** appear as Claude tool parameters.
- They are bound in C# from `ITenantContext` (decoded from JWT `sub` claim) before any tool handler runs.
- Any code that passes tenant identifiers through tool arguments is a security defect — reject the PR.

### 5.2 Booking State Machine
- `pending_deposit → confirmed` happens via Stripe webhook **only**.
- Every status transition must use a guarded `UPDATE bookings SET status = @new WHERE id = @id AND status = @expected` and assert `rowsAffected == 1`.
- Use raw SQL + Dapper (not EF change-tracking) for `CreateBookingHold` and `ClaimWaitlistSlot` — these paths require advisory locks + guarded transitions in a single transaction.

### 5.3 Tenant Scoping
- Every EF Core entity query goes through global query filters keyed on `BusinessId`.
- Every raw SQL statement must include `WHERE business_id = @businessId`.
- Controllers never read `BusinessId` from the request body — only from `ITenantContext`.
- Super-admin cross-tenant reads must use the explicit `IQueryFilterDisabler` wrapper.

### 5.4 Idempotency
- Every Twilio and Stripe webhook handler inserts a row into `processed_events(id, source)` **first** (unique constraint on PK).
- If the insert throws a unique violation → return HTTP 200 immediately without reprocessing.
- Never rely on delivery-once guarantees from external services.

### 5.5 Vertical Config — Not Code
- Adding a new business vertical must **never** require a code change.
- Extend `vertical_config jsonb` and `IVerticalConfigService` for new behavior.
- If a new vertical genuinely requires new booking logic not covered by the resource+offering model, open a discussion before writing code.

### 5.6 Design Tokens
- No hardcoded hex values in component files — always use `var(--accent)`, `var(--bg-surface)`, etc., or Tailwind token aliases.
- No decorative gradients.
- All motion wrapped in `@media (prefers-reduced-motion: reduce)`.
- All interactive elements: `2px solid var(--accent)` focus ring, offset 2px.
- All text must pass WCAG AA contrast against its background.

---

## 6. Scaffold — Phase 0 Output

Zero feature logic. No HTTP calls. No DB writes. Structure + config only.

### Backend (`/backend/`)
- `GhedDay.sln`
- `src/GhedDay.Api/GhedDay.Api.csproj` — refs Application + Infrastructure
- `src/GhedDay.Api/Program.cs` — stubbed `builder.Services.Add*` calls: EF, Identity, SignalR, Hangfire, CORS, Swagger
- `src/GhedDay.Api/appsettings.json` — all required keys: ConnectionStrings, Jwt, Cors, Anthropic, Twilio, Stripe, Hangfire
- `src/GhedDay.Application/GhedDay.Application.csproj` — refs Domain
- `src/GhedDay.Domain/GhedDay.Domain.csproj` — zero external deps
- `src/GhedDay.Infrastructure/GhedDay.Infrastructure.csproj` — NuGet: Npgsql.EntityFrameworkCore.PostgreSQL, Hangfire.PostgreSql, Stripe.net, Twilio, Dapper
- All empty folder trees as spec'd in section 2.2

### Frontend (`/frontend/`)
- `package.json` — Next.js 15, React 19, Tailwind CSS 4, shadcn/ui, framer-motion, `@microsoft/signalr`, `@mux/mux-player-react`, `next-intl`
- `tailwind.config.ts` — extends with design token aliases
- `styles/tokens.css` — all CSS custom properties from section 3.2
- `app/layout.tsx` — Inter font via `next/font/google` (Latin + Vietnamese subsets), `ThemeProvider`
- `app/(marketing)/layout.tsx` + `page.tsx` stubs
- `app/(dashboard)/layout.tsx` + `page.tsx` stubs (auth guard comment only)
- `components/dashboard/ResourceGrid.tsx` stub (not ChairGrid)
- `middleware.ts` stub with auth + refresh comment
- `.env.local.example` — `NEXT_PUBLIC_API_URL`, `NEXT_PUBLIC_SIGNALR_URL`, `NEXT_PUBLIC_MUX_ENV_KEY`

### Root
- `.gitignore`
- `README.md` — setup: `dotnet run` + `npm run dev`
- `.cursor/rules` — all 6 non-negotiables as machine-readable rules

---

## 7. Recommended First Implementation Phase

**Start with Phase 2 (SMS + Claude Loop) immediately after the 1-week foundation.**

The loop is the product. Everything else — Stripe, reminders, dashboard, marketing — is delivery infrastructure around it. Phase 2 is also where the multi-vertical design is most at risk: if `IVerticalConfigService` and the resource-capacity availability query don't work correctly for both verticals under concurrency, nothing else matters.

**The Phase 1 + 2 sequence proves the hardest things first:**
1. The vertical abstraction holds under two real verticals (nail + restaurant)
2. Claude books correctly in both English and Vietnamese
3. Concurrency-safe advisory locks prevent double-booking
4. Twilio signature verification and idempotency work before any money moves

Once a real SMS round-trip works end-to-end for both seeded verticals, the remaining phases are straightforward execution.
