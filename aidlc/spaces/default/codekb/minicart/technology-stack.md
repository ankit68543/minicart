# Technology Stack — MiniCart Microservices

**Assessment Date:** 2026-10-09  
**Project Type:** Microservices Learning Application  

---

## High-Level Technology Matrix

| Layer | Technology | Version | Purpose |
|-------|-----------|---------|---------|
| **Application Runtime** | .NET (3 services) | 6.0+ | Microservices backend |
| **Application Runtime** | Node.js | 18+ | Next.js frontend |
| **Web Framework** | ASP.NET Core | 6.0+ | REST API services |
| **Web Framework** | Next.js | 14+ | Full-stack frontend |
| **UI Library** | React | 18+ | Component framework |
| **Language (Services)** | C# | 11+ | Backend language |
| **Language (Frontend)** | TypeScript | 5.0+ | Type-safe frontend |
| **Build System (Services)** | MSBuild | .NET SDK | .NET build orchestration |
| **Build System (Frontend)** | Node.js build pipeline | npm 10+ | Package management & build |
| **Container Runtime** | Docker Engine | Latest | Local containerization |
| **Orchestration (Dev)** | Docker Compose | 3.9+ | Local development stack |
| **Observability (Traces)** | OpenTelemetry | 1.0+ | Distributed tracing |
| **Observability (Metrics)** | Prometheus | Latest | Metrics collection |
| **Observability (Logs)** | Loki | Latest | Log aggregation |
| **Observability (Traces Storage)** | Tempo | Latest | Trace backend storage |
| **Observability (Visualization)** | Grafana | Latest | Dashboards & analytics |
| **Package Manager** | NuGet | Latest | .NET dependencies |
| **Package Manager** | npm | 10+ | Node.js dependencies |
| **Version Control** | Git | 2.40+ | Code management |
| **Code Editor** | VS Code | 1.80+ | Development environment |
| **Configuration Format** | JSON/YAML | — | Config files |
| **Deployment Target** | Docker Compose / WSL 2 | — | Local development |

---

## Services Technology Breakdown

### CatalogService

**Purpose:** Product catalog microservice  

| Category | Technology | Details |
|----------|-----------|---------|
| **Framework** | ASP.NET Core | 6.0 or later |
| **Language** | C# | 11+ (nullable reference types, records) |
| **Runtime** | .NET CLR | Common Language Runtime |
| **Execution Model** | Async/await | Full async pipeline |
| **HTTP Server** | Kestrel | ASP.NET Core's built-in server |
| **Port** | 5001 | Host: 0.0.0.0 |
| **API Style** | REST | JSON over HTTP |
| **Request Pipeline** | Middleware | Routing → Controllers → Models |
| **Data Access** | In-Memory | No database (seeded collection) |
| **Configuration** | appsettings.json | Per-environment overrides |
| **Logging** | ILogger (built-in) | Structured logging |
| **Telemetry** | OpenTelemetry SDK | Distributed tracing |
| **Dependency Injection** | Built-in IServiceCollection | ASP.NET Core DI container |

**Key Libraries:**
```
Microsoft.AspNetCore.App (implicit)
    ├─ Microsoft.AspNetCore.Mvc (routing, controllers)
    ├─ Microsoft.AspNetCore.Hosting (server)
    └─ Microsoft.Extensions.* (logging, config, DI)

OpenTelemetry
    ├─ OpenTelemetry.Exporter.OpenTelemetryProtocol.Grpc
    └─ OpenTelemetry.Instrumentation.AspNetCore
```

**Build & Deployment:**
```
Build Tool: dotnet CLI
Build Command: dotnet build
Publish Command: dotnet publish -c Release
Container: Multi-stage Dockerfile
Image Size: ~200MB (runtime only)
```

---

### CartService

**Purpose:** Shopping cart management microservice  

| Category | Technology | Details |
|----------|-----------|---------|
| **Framework** | ASP.NET Core | 6.0 or later |
| **Language** | C# | 11+ |
| **Runtime** | .NET CLR | Common Language Runtime |
| **Execution Model** | Async/await | Full async pipeline |
| **HTTP Server** | Kestrel | Async HTTP server |
| **Port** | 5002 | Host: 0.0.0.0 |
| **API Style** | REST | JSON over HTTP |
| **Request Pipeline** | Middleware | Routing → Controllers → Services |
| **Data Access** | In-Memory | Dictionary-based cart storage |
| **Inter-Service Calls** | HttpClient | To CatalogService (with retry/timeout) |
| **Configuration** | appsettings.json | Environment-specific |
| **Logging** | ILogger | Structured, with trace context |
| **Telemetry** | OpenTelemetry SDK | Traces, metrics, logs |
| **Dependency Injection** | Built-in IServiceCollection | ASP.NET Core DI |

**Key Libraries:**
```
Microsoft.AspNetCore.App
    ├─ Microsoft.AspNetCore.Mvc
    ├─ Microsoft.AspNetCore.Hosting
    ├─ System.Net.Http (HttpClient)
    └─ Microsoft.Extensions.*

OpenTelemetry
    ├─ OpenTelemetry.Instrumentation.AspNetCore
    ├─ OpenTelemetry.Instrumentation.Http
    └─ OpenTelemetry.Exporter.OpenTelemetryProtocol.Grpc
```

**Inter-Service Integration:**
```csharp
// Calls CatalogService with trace context propagation
var product = await _httpClient.GetAsync(
    $"{catalogServiceUrl}/api/products/{productId}"
);
// OpenTelemetry automatically creates outgoing span
// and propagates W3C Trace Context header
```

---

### OrderService

**Purpose:** Order processing microservice  

| Category | Technology | Details |
|----------|-----------|---------|
| **Framework** | ASP.NET Core | 6.0 or later |
| **Language** | C# | 11+ |
| **Runtime** | .NET CLR | Common Language Runtime |
| **Execution Model** | Async/await | Full async pipeline |
| **HTTP Server** | Kestrel | Async HTTP server |
| **Port** | 5003 | Host: 0.0.0.0 |
| **API Style** | REST | JSON over HTTP |
| **Request Pipeline** | Middleware | Routing → Controllers → Services |
| **Data Access** | In-Memory | Order ledger (list of orders) |
| **Inter-Service Calls** | HttpClient | To CartService and CatalogService |
| **Configuration** | appsettings.json | Environment-specific |
| **Logging** | ILogger | Structured logging |
| **Telemetry** | OpenTelemetry SDK | Full distributed tracing |
| **Dependency Injection** | Built-in | ASP.NET Core DI container |

**Key Libraries:**
```
Microsoft.AspNetCore.App
    ├─ Microsoft.AspNetCore.Mvc
    ├─ System.Net.Http (HttpClient for service calls)
    └─ Microsoft.Extensions.*

OpenTelemetry
    ├─ OpenTelemetry.Instrumentation.AspNetCore
    ├─ OpenTelemetry.Instrumentation.Http
    └─ OpenTelemetry.Exporter.OpenTelemetryProtocol.Grpc
```

**Inter-Service Orchestration:**
```
OrderService receives request
  ├─ Calls CartService for cart details (HttpClient)
  ├─ Calls CatalogService for each product (HttpClient)
  ├─ Validates order
  └─ Creates Order record
All calls propagate W3C Trace Context for tracing
```

---

### Web Frontend

**Purpose:** Customer-facing UI application  

| Category | Technology | Details |
|----------|-----------|---------|
| **Framework** | Next.js | 14+ (App Router) |
| **UI Library** | React | 18+ |
| **Language** | TypeScript | 5.0+ |
| **Runtime** | Node.js | 18+, LTS |
| **Package Manager** | npm | 10+ (or yarn/pnpm) |
| **Styling** | CSS/Tailwind | Browser CSS + utility classes |
| **HTTP Client** | fetch / axios | Browser APIs or npm package |
| **Telemetry** | OpenTelemetry (Node SDK) | Traces, metrics, logs |
| **Port** | 3000 | Development server |
| **Build Output** | Next.js static/server files | .next directory |
| **Server** | Node.js http server | Built-in via Next.js |
| **API Routes** | Next.js /api | Optional server-side endpoints |
| **State Management** | React hooks + Context | No Redux/Zustand required |
| **Routing** | Next.js App Router | File-based routing |

**Key npm Dependencies:**
```
Core:
  ├─ react (UI components)
  ├─ react-dom (React in browser)
  └─ next (framework)

Telemetry:
  ├─ @opentelemetry/api
  ├─ @opentelemetry/sdk-node
  ├─ @opentelemetry/instrumentation-http
  ├─ @opentelemetry/auto-instrumentations-node
  └─ @opentelemetry/exporter-trace-otlp-grpc

Development:
  ├─ typescript
  ├─ eslint (linting)
  └─ prettier (formatting)
```

**Build & Deployment:**
```
Development: npm run dev
Production Build: npm run build
Production Start: npm start
Container: Multi-stage Dockerfile
Image Size: ~500MB
```

**Integration with Services:**
```typescript
// Fetch with trace context propagation
async function getProducts() {
  const response = await fetch('http://catalog-service:5001/api/products', {
    headers: {
      'traceparent': getTraceContext() // W3C Trace Context
    }
  });
  return response.json();
}
```

---

## Development Environment Stack

### Local Machine Setup

| Tool | Version | Purpose |
|------|---------|---------|
| **Windows** | 11 | Host OS |
| **WSL 2** | 2.0+ | Linux subsystem for Docker |
| **Ubuntu** | 22.04 LTS | Linux distribution in WSL |
| **.NET SDK** | 6.0+ | Compile .NET services |
| **Node.js** | 18+ LTS | Run Next.js frontend |
| **Docker Engine** | Latest | Container runtime (in WSL) |
| **Docker Compose** | 3.9+ | Orchestrate containers |
| **Git** | 2.40+ | Version control |
| **VS Code** | 1.80+ | Code editor |
| **PowerShell 7** | 7.0+ | Command-line shell |

### VS Code Extensions (Recommended)

```
- C# Dev Kit (for .NET development)
- Rest Client (for API testing)
- Docker (for container management)
- GitLens (for git integration)
- OpenTelemetry Collector Config (for config validation)
- YAML (for docker-compose.yml)
```

---

## Container & Deployment Stack

### Container Images

| Service | Base Image | Size |
|---------|-----------|------|
| CatalogService | `mcr.microsoft.com/dotnet/aspnet:6.0` | ~200MB |
| CartService | `mcr.microsoft.com/dotnet/aspnet:6.0` | ~200MB |
| OrderService | `mcr.microsoft.com/dotnet/aspnet:6.0` | ~200MB |
| Web Frontend | `node:18-alpine` | ~200MB |
| OTel Collector | `otel/opentelemetry-collector-contrib:latest` | ~400MB |
| Prometheus | `prom/prometheus:latest` | ~200MB |
| Loki | `grafana/loki:latest` | ~150MB |
| Tempo | `grafana/tempo:latest` | ~150MB |
| Grafana | `grafana/grafana:latest` | ~300MB |

**Total Stack Size:** ~2GB (all containers combined)

### Dockerfile Pattern (ASP.NET Core)

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS builder
WORKDIR /src
COPY CartService.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=builder /app/publish .
EXPOSE 5002
ENTRYPOINT ["dotnet", "CartService.dll"]
```

**Benefits:**
- Multi-stage build reduces final image size
- No SDK in runtime image
- Minimal attack surface

### Dockerfile Pattern (Next.js)

```dockerfile
# Build stage
FROM node:18-alpine AS builder
WORKDIR /app
COPY package*.json .
RUN npm ci --only=production
COPY . .
RUN npm run build

# Runtime stage
FROM node:18-alpine
WORKDIR /app
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/node_modules ./node_modules
EXPOSE 3000
CMD ["npm", "start"]
```

---

## Observability Stack

### OpenTelemetry Components

| Component | Version | Role |
|-----------|---------|------|
| **OpenTelemetry Specification** | 1.0+ | Standard (language-agnostic) |
| **OpenTelemetry SDK for .NET** | Latest | Collect telemetry from services |
| **OpenTelemetry SDK for Node.js** | Latest | Collect telemetry from frontend |
| **Collector (Contrib)** | Latest | Receive, process, export telemetry |
| **OTLP Exporter (gRPC)** | Latest | Send telemetry to Collector |

### Telemetry Backends

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| **Prometheus** | Time-series DB | Latest | Metrics storage & query engine |
| **Loki** | Log aggregation | Latest | Log storage with label indexing |
| **Tempo** | Trace storage | Latest | Distributed trace backend |
| **Grafana** | Visualization | Latest | Unified dashboards |

### Data Flow

```
Services (gRPC via OTLP)
    ↓
OpenTelemetry Collector (receives on :4317)
    ├─ Prometheus exporter → Prometheus :9090
    ├─ Loki exporter → Loki :3100
    └─ Tempo exporter → Tempo :3200
    
Grafana :3000 (queries backends)
    ├─ Prometheus for metrics
    ├─ Loki for logs
    └─ Tempo for traces
```

### Trace Context Standard

**W3C Trace Context (W3C_TRACE_CONTEXT):**
```
traceparent: 00-<trace-id>-<span-id>-<flags>
tracestate: vendor-specific-data
```

**Propagation:**
- Automatic via OpenTelemetry libraries
- All HTTP headers include `traceparent`
- Enables end-to-end visibility

---

## Dependency Management

### NuGet Packages (per .NET Service)

```xml
<ItemGroup>
  <!-- Implicit with ASP.NET Core 6.0+ -->
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
  
  <!-- OpenTelemetry -->
  <PackageReference Include="OpenTelemetry" Version="1.x" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol.Grpc" Version="1.x" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.x" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.x" />
  
  <!-- Additional utilities as needed -->
  <PackageReference Include="System.Net.Http" Version="..." />
</ItemGroup>
```

### npm Dependencies (Frontend)

**Core:**
```json
{
  "dependencies": {
    "react": "^18.0.0",
    "react-dom": "^18.0.0",
    "next": "^14.0.0"
  }
}
```

**Telemetry:**
```json
{
  "dependencies": {
    "@opentelemetry/api": "^1.7.0",
    "@opentelemetry/sdk-node": "^0.43.0",
    "@opentelemetry/auto-instrumentations-node": "^0.39.0",
    "@opentelemetry/exporter-trace-otlp-grpc": "^0.43.0"
  }
}
```

---

## Development Workflow & Tools

### Build Commands

**CatalogService:**
```bash
cd apps/services/CatalogService
dotnet build                              # Compile
dotnet watch run                          # Hot reload
dotnet test                               # Run tests
dotnet publish -c Release                 # Production build
```

**CartService & OrderService:**
```bash
# Same as CatalogService
```

**Web Frontend:**
```bash
cd apps/web
npm install                               # Install dependencies
npm run dev                               # Development (HMR)
npm run build                             # Production build
npm start                                 # Start server
npm test                                  # Run tests
```

**Docker Compose:**
```bash
docker compose build                      # Build all images
docker compose up -d                      # Start all services
docker compose logs -f                    # View logs (all services)
docker compose down                       # Stop and remove
docker compose ps                         # List running services
```

### Testing Frameworks

**ASP.NET Core Services:**
- xUnit (or NUnit) — Unit tests
- Moq — Mocking framework
- FluentAssertions — Assertion library

**Next.js Frontend:**
- Jest — Test runner
- React Testing Library — Component testing
- @testing-library/jest-dom — DOM matchers

---

## Performance Characteristics

### Typical Response Times (Local Development)

| Endpoint | Time | Notes |
|----------|------|-------|
| GET /api/products | 10-50ms | In-memory lookup |
| GET /api/products/{id} | 5-20ms | Single product lookup |
| POST /api/cart/items | 30-100ms | Includes CatalogService call |
| POST /api/orders | 100-200ms | Multiple service calls + validation |

### Memory Footprint (Per Service)

| Service | Baseline | Peak | Growth |
|---------|----------|------|--------|
| CatalogService | ~100MB | ~200MB | Stable (fixed product set) |
| CartService | ~100MB | ~300MB | Grows with active carts |
| OrderService | ~100MB | ~400MB | Grows with order history |
| Web Frontend | ~150MB | ~400MB | Grows with cache |

### Throughput Capacity

**Single Service (estimated):**
- ~1000 requests/sec per service
- Bottleneck: In-memory storage (hashmap lookups)
- Network I/O between services

**Full Stack (local Docker Compose):**
- ~300-500 end-to-end requests/sec
- Constrained by: inter-service latency, trace overhead

**Note:** Learning project — not optimized for production scale.

---

## Security Posture (Learning Project)

| Aspect | Implementation | Notes |
|--------|---|---|
| **Transport Security** | HTTP (no TLS) | Learning environment only |
| **Authentication** | None | Open APIs |
| **Authorization** | None | No role-based access |
| **Input Validation** | Type checking | Basic validation in controllers |
| **Error Handling** | Structured responses | No stack traces exposed |
| **Logging** | Structured JSON | No sensitive data in logs |
| **Secrets** | None used | Configuration via environment |
| **CORS** | Not configured | Single origin (localhost) |

**Future Enhancements:**
- TLS/HTTPS
- JWT authentication
- Role-based access control
- API key validation
- Rate limiting
- WAF (Web Application Firewall)
- Secret management (Vault, Key Vault)

---

## Compliance & Standards

| Standard | Adherence | Notes |
|----------|-----------|-------|
| **OpenTelemetry Spec** | ✅ Full | Uses standard OTLP protocol |
| **W3C Trace Context** | ✅ Full | Traceparent headers propagated |
| **REST API Design** | ✅ Partial | Standard HTTP methods, JSON payloads |
| **12-Factor App** | ✅ Partial | Config via environment, stateless services |
| **Cloud Native** | ✅ Partial | Container-based, orchestrated |
| **Microservices Patterns** | ✅ Partial | Service-per-concern, API communication |

---

## Technology Decision Rationale

| Technology | Why Chosen | Alternatives Considered |
|-----------|-----------|-------------------------|
| **ASP.NET Core** | High performance, modern async, native OTel support | Node.js, Java, Go |
| **Next.js** | Full-stack TypeScript, built-in server, React ecosystem | Svelte, Vue, plain React |
| **OpenTelemetry** | Vendor-neutral, industry standard, multi-language | Datadog, New Relic, proprietary |
| **Prometheus** | Lightweight, CNCF project, time-series expertise | InfluxDB, Graphite |
| **Loki** | Log aggregation optimized for Grafana, label-based | ELK stack, Splunk |
| **Tempo** | Trace storage designed for Grafana, low overhead | Jaeger, Datadog APM |
| **Grafana** | Unified dashboard platform, open source | Kibana, Datadog, New Relic |
| **Docker Compose** | Simplicity for local development, learning-appropriate | Kubernetes (overkill for learning) |

---

## Maturity & Support Status

| Component | Maturity | Support Status |
|-----------|----------|---|
| **.NET 6.0** | Production | LTS until Nov 2024 |
| **ASP.NET Core 6.0** | Production | LTS until Nov 2024 |
| **Next.js 14** | Production | Current/Canary support |
| **React 18** | Production | Active maintenance |
| **OpenTelemetry 1.0+** | Production | CNCF project |
| **Prometheus** | Production | CNCF project |
| **Loki** | Production | Grafana Labs maintained |
| **Tempo** | Production | Grafana Labs maintained |
| **Grafana** | Production | Active development |

**Note:** All components are production-ready. This project uses them in a learning context.
