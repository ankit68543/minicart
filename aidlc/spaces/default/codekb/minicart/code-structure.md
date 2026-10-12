# Code Structure — MiniCart Microservices

**Project Layout:** Monorepo with microservices and observability infrastructure  
**Build System:** Multi-technology (MSBuild for .NET, npm/yarn for Node.js)  

---

## Repository Root Structure

```
minicart/
├── .aidlc/                          # AIDLC workflow state & configuration
│   ├── agents/                      # AI agent definitions for workflow
│   ├── knowledge/                   # Knowledge base for agents
│   ├── sensors/                     # Automated checks
│   ├── tools/                       # Framework CLI tools
│   └── hooks/                       # Lifecycle hooks
│
├── .github/                         # GitHub configuration
│   ├── hooks/                       # Pre-tool-use and stop hooks (AIDLC)
│   ├── skills/                      # Reusable workflow skills
│   └── agents/                      # Agent customizations
│
├── apps/                            # Application code
│   ├── services/                    # Microservices (ASP.NET Core)
│   │   ├── CatalogService/
│   │   ├── CartService/
│   │   └── OrderService/
│   └── web/                         # Frontend (Next.js)
│
├── observability/                   # Telemetry infrastructure configs
│   ├── otel-collector/              # OpenTelemetry Collector config
│   ├── prometheus/                  # Prometheus configuration & rules
│   ├── loki/                        # Loki log aggregation config
│   ├── tempo/                       # Tempo trace backend config
│   └── grafana/                     # Grafana provisioning & dashboards
│
├── tests/                           # Test suite
│   ├── integration/                 # Service-to-service tests
│   ├── e2e/                         # End-to-end scenarios
│   └── smoke/                       # Deployment smoke tests
│
├── docs/                            # Documentation
│   ├── ARCHITECTURE.md
│   ├── AIDLC-WORKFLOW.md
│   ├── OBSERVABILITY.md
│   └── TROUBLESHOOTING.md
│
├── scripts/                         # Automation scripts
│   ├── build.sh                     # Build all services
│   ├── deploy.sh                    # Deploy to Docker
│   ├── test.sh                      # Run all tests
│   └── clean.sh                     # Clean build artifacts
│
├── aidlc/                           # AIDLC intent records
│   ├── active-space → default/
│   └── spaces/
│       └── default/
│           ├── intents/
│           ├── memory/
│           ├── knowledge/
│           └── codekb/              # Reverse engineering output (this file)
│
├── AGENTS.md                        # AI-DLC agent configuration
├── docker-compose.yml               # Container orchestration
├── .env.example                     # Environment template
├── .gitignore                       # Git ignore rules
├── README.md                        # Project overview
└── PROJECT-STATUS.md                # Status & checklist
```

---

## Service Folder Conventions

### ASP.NET Core Service Template (CatalogService, CartService, OrderService)

Each .NET service follows this structure:

```
CartService/
├── Program.cs                       # Entry point, dependency injection setup
├── appsettings.json                 # Configuration (dev)
├── appsettings.Production.json       # Configuration (prod)
├── CartService.csproj               # Project file (MSBuild)
├── Dockerfile                       # Container image definition
│
├── Controllers/                     # HTTP endpoints
│   └── CartController.cs            # [HttpGet], [HttpPost], etc.
│
├── Models/                          # Data models
│   ├── Cart.cs
│   ├── CartItem.cs
│   └── ApiResponse.cs
│
├── Services/                        # Business logic
│   ├── CartService.cs               # Cart operations
│   ├── CatalogServiceClient.cs       # HTTP client to CatalogService
│   └── TelemetryService.cs           # OpenTelemetry instrumentation
│
├── Middleware/                      # Request/response processing
│   ├── ExceptionHandlingMiddleware.cs
│   └── LoggingMiddleware.cs
│
├── Config/                          # Configuration classes
│   ├── OpenTelemetryConfig.cs        # OTEL setup
│   └── ServiceUrls.cs                # Service URLs from env vars
│
└── Tests/                           # Unit tests
    ├── CartControllerTests.cs
    └── CartServiceTests.cs
```

---

## Next.js Frontend Structure

```
web/
├── app/                             # Next.js App Router (14+)
│   ├── page.tsx                     # Home page
│   ├── layout.tsx                   # Root layout
│   ├── products/
│   │   ├── page.tsx                 # Product listing
│   │   └── [id]/
│   │       └── page.tsx             # Product detail
│   ├── cart/
│   │   └── page.tsx                 # Cart view
│   ├── checkout/
│   │   └── page.tsx                 # Checkout flow
│   └── api/                         # API routes (optional, for aggregation)
│       ├── products/route.ts
│       ├── cart/route.ts
│       └── orders/route.ts
│
├── components/                      # React components
│   ├── ProductCard.tsx
│   ├── CartItem.tsx
│   ├── Header.tsx
│   ├── Footer.tsx
│   └── checkout/
│       └── CheckoutForm.tsx
│
├── lib/                             # Utilities
│   ├── api/
│   │   ├── catalogClient.ts         # Calls CatalogService
│   │   ├── cartClient.ts            # Calls CartService
│   │   ├── orderClient.ts           # Calls OrderService
│   │   └── httpClient.ts            # HTTP wrapper with trace context
│   ├── telemetry/
│   │   ├── instrumentation.ts       # OpenTelemetry setup for browser
│   │   └── traceContext.ts          # Trace context management
│   └── types/
│       └── models.ts                # TypeScript interfaces
│
├── public/                          # Static assets
│   └── images/
│
├── styles/                          # CSS/styling
│   └── globals.css
│
├── .env.local                       # Local development env vars
├── next.config.js                   # Next.js configuration
├── tsconfig.json                    # TypeScript configuration
├── package.json                     # Dependencies & scripts
├── package-lock.json                # Dependency lock file
└── Dockerfile                       # Container image definition
```

---

## Observability Configuration Structure

### OTEL Collector Configuration

```
observability/otel-collector/
├── config.yml                       # Main OTel Collector config
│   ├── receivers:                   # Listen for telemetry (gRPC, HTTP)
│   ├── processors:                  # Process & enrich telemetry
│   └── exporters:                   # Send to backends (Prometheus, Loki, Tempo)
```

### Prometheus Configuration

```
observability/prometheus/
├── prometheus.yml                   # Scrape targets, retention, etc.
│   ├── global:                      # Global settings (scrape interval)
│   ├── scrape_configs:              # Which endpoints to scrape
│   └── alerting:                    # Alert rules (optional)
├── rules/                           # Alert & recording rules
│   └── minicart-rules.yml            # Custom metrics (request rate, latency)
└── storage/                         # Time-series database (tsdb)
```

### Loki Configuration

```
observability/loki/
├── loki-config.yml                  # Loki server config
│   ├── auth_enabled:                # Access control
│   ├── ingester:                    # Log ingestion settings
│   └── storage_config:              # Backend storage
└── storage/                         # Log database
```

### Tempo Configuration

```
observability/tempo/
├── tempo-config.yml                 # Tempo server config
│   ├── receiver:                    # Accept traces
│   └── storage:                     # Trace storage backend
└── storage/                         # Trace database
```

### Grafana Provisioning

```
observability/grafana/
├── provisioning/
│   ├── dashboards/                  # Dashboard JSON files
│   │   ├── overview.json            # System overview dashboard
│   │   ├── services.json            # Per-service dashboard
│   │   └── traces.json              # Trace explorer dashboard
│   ├── datasources/                 # Data source configs
│   │   ├── prometheus.yml
│   │   ├── loki.yml
│   │   └── tempo.yml
│   └── notifiers/                   # Alert notification config
└── storage/                         # Grafana database
```

---

## File Naming & Conventions

### .NET Services

| Category | Pattern | Example |
|----------|---------|---------|
| Classes | PascalCase | `ProductController.cs`, `CartService.cs` |
| Methods | PascalCase | `GetProduct()`, `AddToCart()` |
| Properties | PascalCase | `ProductId`, `CartTotal` |
| Variables | camelCase | `productList`, `cartItems` |
| Constants | UPPER_SNAKE_CASE | `MAX_CART_SIZE`, `HEALTH_CHECK_TIMEOUT` |
| Namespaces | PascalCase hierarchical | `MiniCart.Services.Cart`, `MiniCart.Models` |
| Interfaces | Prefix with `I` | `ICartService`, `ICatalogClient` |

### Next.js / TypeScript Frontend

| Category | Pattern | Example |
|----------|---------|---------|
| React components | PascalCase | `ProductCard.tsx`, `CartItem.tsx` |
| Pages | kebab-case in dirs | `products/[id]/page.tsx` |
| Utilities | camelCase | `httpClient.ts`, `traceContext.ts` |
| Types/Interfaces | PascalCase | `Product`, `Cart`, `OrderResponse` |
| Constants | UPPER_SNAKE_CASE | `API_BASE_URL`, `TRACE_HEADER_NAME` |
| Hooks | Prefix with `use` | `useCart.ts`, `useProducts.ts` |
| CSS/Tailwind | kebab-case | `.product-card`, `.cart-item` |

### Configuration Files

| File | Purpose | Format |
|------|---------|--------|
| `appsettings.json` | Service configuration (dev) | JSON |
| `docker-compose.yml` | Container orchestration | YAML |
| `.env.example` | Environment variable template | Shell (.env) |
| `prometheus.yml` | Prometheus scrape config | YAML |
| `otel-collector-config.yml` | OTel Collector config | YAML |
| `tsconfig.json` | TypeScript config | JSON |
| `package.json` | Node.js dependencies | JSON |

---

## Build Artifact Directories

These are created during build but NOT committed to git:

### .NET Services
```
CartService/
├── bin/                             # Compiled assemblies
│   ├── Debug/
│   │   ├── net6.0/
│   │   │   ├── CartService.dll
│   │   │   └── *.dll (dependencies)
│   │   └── net6.0/publish/          # For Docker image
│   └── Release/
│
└── obj/                             # Intermediate build objects
    └── Debug/
```

### Next.js Frontend
```
web/
├── node_modules/                    # npm packages (excluded from git)
├── .next/                           # Build output
│   ├── server/
│   ├── static/
│   └── build-manifest.json
├── dist/                            # Exported static HTML (optional)
└── out/                             # Static export output (if configured)
```

---

## Development Workflow Structure

### Local Environment

Each developer creates/uses:

```
minicart/ (cloned repo)
├── .env.local                       # Local overrides (not in git)
├── docker-compose.override.yml      # Local compose overrides (optional)
└── [service]/.env.local             # Per-service overrides
```

### Continuous Integration

```
.github/
├── workflows/                       # GitHub Actions (optional)
│   ├── build.yml                    # Builds all services
│   ├── test.yml                     # Runs tests
│   └── deploy.yml                   # Deploys to staging
└── hooks/                           # AIDLC lifecycle hooks
    └── aidlc.json                   # Hook configuration
```

---

## Module Organization by Concern

### CatalogService Modules

```
Models/
  ├── Product.cs              # Product data model
  └── ApiResponse.cs          # Standard API response

Controllers/
  └── ProductsController.cs   # HTTP endpoints: GET /api/products

Services/
  └── ProductService.cs       # Product lookup, filtering

Config/
  ├── ServiceConfiguration.cs # Dependency injection setup
  └── OpenTelemetryConfig.cs  # OTEL instrumentation
```

### CartService Modules

```
Models/
  ├── Cart.cs                 # Cart aggregate
  ├── CartItem.cs             # Item in cart
  └── ApiResponse.cs          # Standard responses

Controllers/
  └── CartController.cs       # HTTP endpoints: POST/GET/PUT/DELETE

Services/
  ├── CartService.cs          # Cart operations (add, remove, update)
  ├── CatalogServiceClient.cs # HTTP client to CatalogService
  └── TelemetryService.cs     # OTEL instrumentation helpers

Config/
  ├── ServiceUrls.cs          # Environment variable resolution
  └── OpenTelemetryConfig.cs  # OTEL setup
```

### OrderService Modules

```
Models/
  ├── Order.cs                # Order aggregate
  ├── OrderItem.cs            # Item in order
  ├── OrderStatus.cs          # Enum: Pending, Confirmed, etc.
  └── ApiResponse.cs

Controllers/
  └── OrderController.cs      # HTTP endpoints: POST /api/orders

Services/
  ├── OrderService.cs         # Order creation, validation
  ├── CartServiceClient.cs    # HTTP client to CartService
  ├── CatalogServiceClient.cs # HTTP client to CatalogService
  └── TelemetryService.cs     # OTEL instrumentation

Config/
  ├── ServiceUrls.cs          # Service URLs from env
  └── OpenTelemetryConfig.cs
```

### Next.js Frontend Modules

```
app/
  ├── page.tsx                # Home/products listing
  ├── products/[id]/page.tsx  # Product detail
  ├── cart/page.tsx           # Cart view
  └── checkout/page.tsx       # Checkout & order creation

lib/api/
  ├── catalogClient.ts        # Call CatalogService
  ├── cartClient.ts           # Call CartService
  ├── orderClient.ts          # Call OrderService
  └── httpClient.ts           # HTTP wrapper with trace context

lib/telemetry/
  ├── instrumentation.ts      # OTEL setup (browser-side)
  └── traceContext.ts         # Generate/propagate W3C Trace Context
```

---

## Dependency Flow

### Services Dependency Graph

```
CartService
  ├─→ CatalogService (for product validation)
  └─→ OTel Collector (for telemetry)

OrderService
  ├─→ CartService (for cart details)
  ├─→ CatalogService (for product validation)
  └─→ OTel Collector (for telemetry)

Next.js Frontend
  ├─→ CatalogService (for product list)
  ├─→ CartService (for cart operations)
  ├─→ OrderService (for order creation)
  └─→ OTel Collector (for browser traces) via browser SDK
```

### No Circular Dependencies

- CatalogService has no dependencies on other services (data source)
- CartService depends on CatalogService (validation)
- OrderService depends on both CartService and CatalogService
- Frontend calls all three services

**Result:** Clear dependency hierarchy, no circular calls

---

## Hot-Reload / Development Mode

### .NET Services

```bash
dotnet watch run --project CartService/CartService.csproj
```

**Effect:** Automatically restarts service when code changes  
**Port:** Service remains on configured port (5002)  
**Logs:** Output to console  

### Next.js Frontend

```bash
cd apps/web && npm run dev
```

**Effect:** Hot Module Replacement (HMR) on code changes  
**Port:** Frontend on 3000  
**Logs:** Output to console  

### Docker Compose Override (Optional)

```yaml
# docker-compose.override.yml
services:
  cart-service:
    volumes:
      - ./apps/services/CartService:/app
      # Volume mount for live code reload in container
```

---

## Configuration Sources (Precedence)

Each service resolves configuration in this order:

1. **Environment Variables** (highest priority)
   - `ASPNETCORE_URLS`
   - `SERVICE_NAME`
   - `OTEL_EXPORTER_OTLP_ENDPOINT`
   - etc.

2. **appsettings.json**
   - Default configuration

3. **appsettings.{Environment}.json**
   - Override by ASPNETCORE_ENVIRONMENT (Development, Production)

4. **Hardcoded Defaults** (lowest priority)
   - Fallback if nothing else is set

**Example Resolution:**
```
ASPNETCORE_URLS env var exists?
  → Use it
  → Use appsettings.Production.json["Kestrel"]["Endpoints"]
    → Use appsettings.json["Kestrel"]["Endpoints"]
      → Use default (http://0.0.0.0:5002)
```

---

## Container Image Layer Structure

### .NET Service Dockerfile

```dockerfile
# Base image layer
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS builder

# Build layer
WORKDIR /src
COPY CartService.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime layer
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=builder /app/publish .
ENTRYPOINT ["dotnet", "CartService.dll"]
```

**Result:** Multi-stage build, minimal runtime image  
**Size:** ~200MB (runtime) vs ~2GB (SDK)  

### Next.js Frontend Dockerfile

```dockerfile
# Build stage
FROM node:18-alpine AS builder
WORKDIR /app
COPY package*.json .
RUN npm ci
COPY . .
RUN npm run build

# Runtime stage
FROM node:18-alpine
WORKDIR /app
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/node_modules ./node_modules
COPY --from=builder /app/package.json .
EXPOSE 3000
CMD ["npm", "start"]
```

**Result:** Multi-stage build, optimized runtime  
**Size:** ~500MB (vs ~2GB with all dev dependencies)  

---

## File Organization Principles

1. **Separation of Concerns** — Each folder has a single responsibility
2. **DRY (Don't Repeat Yourself)** — Shared utilities in `lib/` or `Services/`
3. **Testability** — Each module can be unit tested independently
4. **Scalability** — Easy to add new services, pages, or features
5. **Discoverability** — Clear naming makes it obvious where code lives
6. **Maintainability** — Consistent structure across all services
