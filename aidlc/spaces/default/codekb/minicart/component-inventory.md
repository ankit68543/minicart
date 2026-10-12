# Component Inventory — MiniCart Microservices

**Scan Date:** 2026-10-09  
**Inventory Type:** Complete component registry by category  

---

## Application Services (Business Logic)

### CatalogService

| Aspect | Value |
|--------|-------|
| **Type** | Microservice (API) |
| **Technology** | ASP.NET Core 6.0+ |
| **Port** | 5001 |
| **Network** | `minicart-app` |
| **Runtime** | .NET CLR |
| **Language** | C# |
| **Purpose** | Product catalog and pricing |
| **Data Storage** | In-memory (seed data) |
| **External Dependencies** | OpenTelemetry Collector |
| **Health Check** | GET /health |
| **Container Image** | `minicart-catalog` (built from Dockerfile) |
| **Environment Variables** | `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`, `SERVICE_NAME`, `OTEL_EXPORTER_OTLP_ENDPOINT` |

**Key Endpoints:**
- `GET /health`
- `GET /api/products`
- `GET /api/products/{id}`
- `GET /api/products/search?category={category}`

**Data Model:** Product (id, name, description, price, stockLevel, category, createdAt)

---

### CartService

| Aspect | Value |
|--------|-------|
| **Type** | Microservice (API) |
| **Technology** | ASP.NET Core 6.0+ |
| **Port** | 5002 |
| **Network** | `minicart-app` |
| **Runtime** | .NET CLR |
| **Language** | C# |
| **Purpose** | Shopping cart management |
| **Data Storage** | In-memory (per-session) |
| **External Dependencies** | CatalogService (HTTP), OpenTelemetry Collector |
| **Health Check** | GET /health |
| **Container Image** | `minicart-cart` (built from Dockerfile) |
| **Environment Variables** | `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`, `SERVICE_NAME`, `CATALOG_SERVICE_URL`, `OTEL_EXPORTER_OTLP_ENDPOINT` |

**Key Endpoints:**
- `GET /health`
- `POST /api/cart`
- `GET /api/cart/{cartId}`
- `POST /api/cart/{cartId}/items`
- `PUT /api/cart/{cartId}/items/{itemId}`
- `DELETE /api/cart/{cartId}/items/{itemId}`
- `GET /api/cart/{cartId}/total`

**Data Model:** Cart (id, customerId, items[], total, createdAt, lastModified)  
**Item Model:** CartItem (id, productId, quantity, priceAtTime)

---

### OrderService

| Aspect | Value |
|--------|-------|
| **Type** | Microservice (API) |
| **Technology** | ASP.NET Core 6.0+ |
| **Port** | 5003 |
| **Network** | `minicart-app` |
| **Runtime** | .NET CLR |
| **Language** | C# |
| **Purpose** | Order processing and fulfillment |
| **Data Storage** | In-memory (order ledger) |
| **External Dependencies** | CartService (HTTP), CatalogService (HTTP), OpenTelemetry Collector |
| **Health Check** | GET /health |
| **Container Image** | `minicart-order` (built from Dockerfile) |
| **Environment Variables** | `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`, `SERVICE_NAME`, `CART_SERVICE_URL`, `CATALOG_SERVICE_URL`, `OTEL_EXPORTER_OTLP_ENDPOINT` |

**Key Endpoints:**
- `GET /health`
- `POST /api/orders`
- `GET /api/orders/{orderId}`
- `GET /api/orders/customer/{customerId}`
- `PUT /api/orders/{orderId}/status`

**Data Model:** Order (id, cartId, customerId, items[], total, status, shippingAddress, createdAt)  
**Item Model:** OrderItem (productId, quantity, price)  
**Status Enum:** Pending, Confirmed, Processing, Shipped, Delivered, Cancelled

---

### Web Frontend

| Aspect | Value |
|--------|-------|
| **Type** | Web Application (UI) |
| **Technology** | Next.js 14+, React 18+, TypeScript |
| **Port** | 3000 |
| **Network** | `minicart-app` |
| **Runtime** | Node.js 18+ |
| **Language** | TypeScript, JSX |
| **Purpose** | Customer-facing user interface |
| **Data Storage** | Browser LocalStorage (optional), session state |
| **External Dependencies** | CatalogService (HTTP), CartService (HTTP), OrderService (HTTP), OpenTelemetry Collector |
| **Health Check** | GET / (HTTP 200) |
| **Container Image** | `minicart-web` (built from Dockerfile) |
| **Environment Variables** | `NEXT_PUBLIC_API_URL`, `CATALOG_SERVICE_URL`, `CART_SERVICE_URL`, `ORDER_SERVICE_URL`, `OTEL_EXPORTER_OTLP_ENDPOINT` |

**Key Pages:**
- `/` — Home / Product listing
- `/products` — Product catalog
- `/cart` — Shopping cart view
- `/checkout` — Order creation
- `/orders` — Order history

---

## Data Storage Components

### In-Memory Stores (No Database)

| Component | Service | Data | Scope |
|-----------|---------|------|-------|
| Product Cache | CatalogService | Products (100+ items) | Application lifetime |
| Shopping Carts | CartService | Multiple carts (per session) | Cart lifetime (not persisted) |
| Orders Ledger | OrderService | Orders (historical) | Application lifetime |

**Note:** Learning project design — no database persistence.  
**For Production:** Replace with PostgreSQL, MongoDB, or Redis.

---

## Networking Components

### Docker Networks

| Network | Name | Driver | Purpose |
|---------|------|--------|---------|
| Application | `minicart-app` | Bridge | Inter-service communication (services only) |
| Observability | `minicart-observability` | Bridge | Telemetry infrastructure (isolated) |

### Network Services

| Service | Network | Port | Protocol |
|---------|---------|------|----------|
| OpenTelemetry Collector | Both (bridge) | 4317 | gRPC (OTLP) |
| OpenTelemetry Collector | Both (bridge) | 4318 | HTTP/1.1 (OTLP) |
| Prometheus | `minicart-observability` | 9090 | HTTP |
| Loki | `minicart-observability` | 3100 | HTTP |
| Tempo | `minicart-observability` | 3200 | gRPC/HTTP |
| Grafana | `minicart-observability` | 3000 | HTTP |

---

## Observability Infrastructure

### Telemetry Backends

| Component | Purpose | Technology | Port | Storage | Retention |
|-----------|---------|------------|------|---------|-----------|
| **Prometheus** | Metrics aggregation | Time-series DB | 9090 | Local disk (TSDB) | 30 days (configurable) |
| **Loki** | Log aggregation | Log storage | 3100 | Local disk | 30 days (configurable) |
| **Tempo** | Trace storage | Trace backend | 3200 | Local disk | Configurable |
| **Grafana** | Visualization | Dashboard platform | 3000 | Embedded DB | — |

### OpenTelemetry Collector

| Aspect | Details |
|--------|---------|
| **Image** | `otel/opentelemetry-collector-contrib:latest` |
| **Ports** | 4317 (gRPC), 4318 (HTTP), 8888, 8889 |
| **Role** | Receives telemetry from services, exports to backends |
| **Receivers** | OTLP gRPC, OTLP HTTP |
| **Exporters** | Prometheus, Loki, Tempo |
| **Networks** | Both `app` and `observability` (bridge) |

---

## Message Routing & Communication

### Inter-Service HTTP Calls

| Caller | Callee | Purpose | Protocol |
|--------|--------|---------|----------|
| Web Frontend | CatalogService | Get product list | REST/HTTP |
| Web Frontend | CartService | Manage cart | REST/HTTP |
| Web Frontend | OrderService | Create order | REST/HTTP |
| CartService | CatalogService | Validate products | REST/HTTP |
| OrderService | CartService | Get cart details | REST/HTTP |
| OrderService | CatalogService | Validate products | REST/HTTP |

### Telemetry Flow

```
Service (app network)
  ↓ OTLP/gRPC
OTel Collector (both networks)
  ├→ Prometheus :9090 (observability network)
  ├→ Loki :3100 (observability network)
  └→ Tempo :3200 (observability network)
```

---

## Configuration Management

### Environment Variables (Per Service)

| Variable | Source | Used By | Purpose |
|----------|--------|---------|---------|
| `ASPNETCORE_ENVIRONMENT` | docker-compose.yml | .NET services | Development/Production mode |
| `ASPNETCORE_URLS` | docker-compose.yml | .NET services | Server bind address/port |
| `SERVICE_NAME` | docker-compose.yml | All services | Service identifier in telemetry |
| `SERVICE_VERSION` | docker-compose.yml | All services | Version in telemetry attributes |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | docker-compose.yml | All services | OTel Collector address |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | docker-compose.yml | All services | gRPC or HTTP |
| `CATALOG_SERVICE_URL` | docker-compose.yml | CartService, OrderService | HTTP address to CatalogService |
| `CART_SERVICE_URL` | docker-compose.yml | OrderService | HTTP address to CartService |
| `NEXT_PUBLIC_API_URL` | docker-compose.yml | Web Frontend | Frontend API base URL |

### Configuration Files (Checked into Git)

| File | Service | Purpose |
|------|---------|---------|
| `appsettings.json` | .NET services | Default configuration |
| `appsettings.Production.json` | .NET services | Production overrides |
| `Dockerfile` | All services | Container image definition |
| `docker-compose.yml` | Orchestration | Service and network definitions |
| `.env.example` | All services | Environment variable template |
| `prometheus.yml` | Prometheus | Scrape targets and retention |
| `otel-collector-config.yml` | OTel Collector | Receivers, processors, exporters |
| `loki-config.yml` | Loki | Log storage and retention |
| `tempo-config.yml` | Tempo | Trace storage backend |

---

## Dependency Graph

### Build-Time Dependencies

**CatalogService:**
- Microsoft.AspNetCore.App (target framework)
- OpenTelemetry (instrumentation)
- System.* libraries (standard)

**CartService:**
- Microsoft.AspNetCore.App
- OpenTelemetry
- System.Net.Http (for CatalogService calls)
- System.* libraries

**OrderService:**
- Microsoft.AspNetCore.App
- OpenTelemetry
- System.Net.Http (for inter-service calls)
- System.* libraries

**Web Frontend:**
- Next.js (framework)
- React (UI library)
- TypeScript (language)
- @opentelemetry/* (instrumentation)
- npm packages (dependencies)

### Runtime Dependencies

**Each .NET Service:**
- .NET Runtime 6.0+
- OpenTelemetry SDK
- Network access to OTel Collector

**Web Frontend:**
- Node.js 18+
- npm packages (installed)
- Network access to services and OTel Collector

**OTel Collector:**
- Prometheus instance
- Loki instance
- Tempo instance
- Network connectivity to all three

**Observability Stack:**
- Prometheus (reads Collector and self-metrics)
- Loki (receives logs from Collector)
- Tempo (receives traces from Collector)
- Grafana (queries all three)

---

## Deployment Topology

### Docker Compose Services (Complete List)

| Container Name | Service | Image | Port | Network(s) |
|---|---|---|---|---|
| minicart-catalog | CatalogService | Built | 5001 | app |
| minicart-cart | CartService | Built | 5002 | app |
| minicart-order | OrderService | Built | 5003 | app |
| minicart-web | Web Frontend | Built | 3000 | app |
| minicart-otel-collector | OTel Collector | otel/opentelemetry-collector-contrib | 4317, 4318 | app + observability |
| minicart-prometheus | Prometheus | prom/prometheus | 9090 | observability |
| minicart-loki | Loki | grafana/loki | 3100 | observability |
| minicart-tempo | Tempo | grafana/tempo | 3200 | observability |
| minicart-grafana | Grafana | grafana/grafana | 3000 | observability |

**Total Services:** 9 containers  
**Application Services:** 4 (Cart, Catalog, Order, Web)  
**Observability Services:** 5 (Collector, Prometheus, Loki, Tempo, Grafana)  

---

## Resource Allocations (Docker)

### Compute

| Component | CPU | Memory | Rationale |
|-----------|-----|--------|-----------|
| CatalogService | Low (no constraints) | ~256MB | In-memory products, minimal processing |
| CartService | Low | ~256MB | In-memory carts, frequent reads |
| OrderService | Low | ~256MB | Order processing, validation |
| Web Frontend | Low | ~256MB | Node.js server, static assets |
| OTel Collector | Medium | ~512MB | Processing telemetry from all services |
| Prometheus | Medium | ~512MB | Time-series storage and query |
| Loki | Medium | ~512MB | Log aggregation and search |
| Tempo | Medium | ~512MB | Trace storage and querying |
| Grafana | Low | ~256MB | Visualization and queries |

**Total:** ~4GB-5GB for full stack (learning environment)

---

## Third-Party Dependencies

### Package Registry

| Framework | Registry | Tool |
|-----------|----------|------|
| .NET Core | NuGet | dotnet restore |
| Node.js | npm | npm install |

### Typical NuGet Packages (per .NET service)

```
- Microsoft.AspNetCore.App (implicit, net6.0+)
- OpenTelemetry (core)
- OpenTelemetry.Exporter.OpenTelemetryProtocol.Grpc
- OpenTelemetry.Instrumentation.AspNetCore
- System.Net.Http (for HttpClient)
```

### Typical npm Packages (frontend)

```
- react (18+)
- next (14+)
- typescript
- @opentelemetry/api
- @opentelemetry/sdk-node
- @opentelemetry/auto-instrumentations-node
- @opentelemetry/exporter-trace-otlp-grpc
```

---

## Integration Points

### External Services (Required)

| Service | Port | Protocol | Required? | Fallback |
|---------|------|----------|-----------|----------|
| OpenTelemetry Collector | 4317 | gRPC | Yes* | Logs to stdout if unavailable |
| Prometheus | 9090 | HTTP | No | Metrics lost (telemetry still works) |
| Loki | 3100 | HTTP | No | Logs lost (telemetry still works) |
| Tempo | 3200 | gRPC/HTTP | No | Traces lost (telemetry still works) |
| Grafana | 3000 | HTTP | No | No dashboards (services still run) |

**Note:** Services remain operational if observability backends are down (graceful degradation).

---

## API Contracts Summary

### Standard Response Format (All Services)

```json
{
  "success": true|false,
  "data": { /* response payload */ },
  "error": "string" (if success=false),
  "message": "string" (additional details),
  "timestamp": "ISO8601"
}
```

### Standard Trace Context Header

```
traceparent: 00-<trace-id>-<span-id>-<flags>
```

### Health Check Format

```json
{
  "status": "healthy|degraded|unhealthy",
  "service": "service-name",
  "version": "1.0.0",
  "timestamp": "ISO8601"
}
```

---

## Observability Metrics Captured

### Automatic (via OTel Instrumentation)

- HTTP server requests (count, duration, errors)
- HTTP client calls (count, duration, errors)
- Span creation and propagation
- Service attributes (name, version, environment)

### Custom (per service)

- Product queries (CatalogService)
- Cart operations (CartService)
- Order creation rate (OrderService)
- Validation failures
- Cache hits/misses
- Business metrics (orders/min, cart size)

### Aggregated by Prometheus

- `http_requests_total` (counter)
- `http_request_duration_seconds` (histogram)
- `http_request_errors_total` (counter)
- Custom business metrics with labels (service, endpoint, status)

---

## Security Components (Learning Project)

**Note:** This is a learning project with simplified security.

| Aspect | Implementation | Notes |
|--------|---|---|
| **Authentication** | None | Open APIs for simplicity |
| **Authorization** | None | No role-based access control |
| **Encryption (Transit)** | HTTP only | No TLS in local environment |
| **Data Validation** | Input validation on requests | Type checking, range checks |
| **Error Handling** | Structured error responses | No stack traces exposed |
| **CORS** | Not configured | Single origin (localhost) |

**For Production:** Add JWT, RBAC, TLS, API keys, rate limiting.
