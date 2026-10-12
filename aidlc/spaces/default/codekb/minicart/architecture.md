# Architecture — MiniCart Microservices

**Date Scanned:** 2026-10-09  
**Architecture Pattern:** Microservices with Distributed Observability  
**Communication Style:** Synchronous REST/HTTP with OpenTelemetry tracing  

---

## System Architecture Diagram

```
┌────────────────────────────────────────────────────────────────┐
│                    CLIENT LAYER                                │
│                    (Web Browser)                               │
└──────────────────────────┬─────────────────────────────────────┘
                           │ HTTP/HTTPS
                           │
        ┌──────────────────▼──────────────────┐
        │                                      │
        │    APPLICATION SERVICES NETWORK     │
        │       (Docker: minicart-app)        │
        │                                      │
        │   ┌────────────────────────────┐    │
        │   │   Web (Next.js Frontend)   │    │
        │   │   Port: 3000               │    │
        │   │   - React components       │    │
        │   │   - Product listing        │    │
        │   │   - Cart UI                │    │
        │   │   - Checkout flow          │    │
        │   └────────┬────────┬──────────┘    │
        │            │        │               │
        │   ┌────────▼─┐  ┌───▼──────────┐   │
        │   │ Catalog  │  │    Cart      │   │
        │   │ Service  │  │   Service    │   │
        │   │ 5001     │  │   5002       │   │
        │   │ (.NET)   │  │  (.NET)      │   │
        │   └──────────┘  └─────┬────────┘   │
        │                       │            │
        │                  ┌────▼──────────┐ │
        │                  │    Order      │ │
        │                  │   Service     │ │
        │                  │   5003        │ │
        │                  │   (.NET)      │ │
        │                  └───────────────┘ │
        │                                    │
        └────────────────────┬───────────────┘
                             │ OTLP (gRPC)
                             │ 0.0.0.0:4317
        ┌────────────────────▼───────────────────┐
        │                                         │
        │  OBSERVABILITY SERVICES NETWORK        │
        │     (Docker: minicart-observability)   │
        │                                         │
        │  ┌──────────────────────────────────┐  │
        │  │ OpenTelemetry Collector          │  │
        │  │ (Receives traces, metrics, logs) │  │
        │  │ gRPC: 4317 | HTTP: 4318         │  │
        │  └───┬──────────────┬───────────────┘  │
        │      │              │                  │
        │  ┌───▼──┐    ┌──────▼──────┐    ┌────▼────┐
        │  │ Prom │    │ Loki        │    │ Tempo   │
        │  │ 9090 │    │ 3100        │    │ 3200    │
        │  │(met) │    │ (logs)      │    │(traces) │
        │  └──────┘    └─────────────┘    └─────────┘
        │      │              │                 │
        │      └──────────┬───┴─────────────────┘
        │                 │
        │          ┌──────▼─────┐
        │          │  Grafana   │
        │          │  3000      │
        │          │(dashboards)│
        │          └────────────┘
        │                                         │
        └─────────────────────────────────────────┘
```

---

## Network Architecture

### Application Network (`minicart-app`)

**Purpose:** Isolated network for application services only  
**Services:**
- `web` (3000) — Next.js frontend
- `catalog-service` (5001) — Product catalog
- `cart-service` (5002) — Shopping cart
- `order-service` (5003) — Order processing

**Communication Rules:**
- Services communicate via Docker DNS: `<service-name>:<port>`
- Example: CartService calls CatalogService at `http://catalog-service:5001`
- No direct access to observability services
- All services emit telemetry to OTel Collector at `otel-collector:4317`

### Observability Network (`minicart-observability`)

**Purpose:** Isolated network for monitoring and analytics  
**Services:**
- `otel-collector` (4317/4318) — Telemetry ingestion hub
- `prometheus` (9090) — Metrics storage
- `loki` (3100) — Log aggregation
- `tempo` (3200) — Distributed trace storage
- `grafana` (3000) — Visualization

**Communication Rules:**
- Observability services are NOT accessible from application network
- OTel Collector bridges both networks (connected to both)
- Metrics, logs, and traces flow one-way: app → collector → backends → Grafana

### Bridge Communication

```
App Services → OTLP/gRPC to otel-collector:4317 (both networks)
                ↓
         otel-collector processes telemetry
                ↓
         Exports to:
           • prometheus:9090 (metrics)
           • loki:3100 (logs)
           • tempo:3200 (traces)
                ↓
         Grafana queries all three ← (dashboards)
```

---

## Service Definitions

### CatalogService (Port 5001)

**Technology:** ASP.NET Core 6.0+  
**Runtime:** .NET CLR  
**Build System:** MSBuild / dotnet CLI  
**Role:** Master product repository  

**Responsibilities:**
- Maintain product catalog (in-memory)
- Serve product list queries
- Provide product details and pricing
- Report product availability

**Data Model:**
```csharp
public class Product {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int StockLevel { get; set; }
    public string Category { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**API Endpoints:**
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/health` | Health check |
| GET | `/api/products` | List all products |
| GET | `/api/products/{id}` | Get product by ID |
| GET | `/api/products/search?category={cat}` | Search by category |

**Dependencies:**
- `OpenTelemetry.Exporter.OpenTelemetryProtocol.Grpc`
- `OpenTelemetry.Instrumentation.AspNetCore`
- `System.Net.Http` (for service-to-service calls if needed)

**Telemetry:**
- Automatic HTTP server spans
- Custom span for product lookup
- Metrics: request count, latency histogram
- Logs: structured JSON with trace context

---

### CartService (Port 5002)

**Technology:** ASP.NET Core 6.0+  
**Runtime:** .NET CLR  
**Build System:** MSBuild / dotnet CLI  
**Role:** Shopping cart management  

**Responsibilities:**
- Create and manage customer shopping carts
- Validate products and availability
- Calculate totals
- Support add/remove/update operations

**Data Model:**
```csharp
public class Cart {
    public string Id { get; set; }  // Unique cart ID
    public string CustomerId { get; set; }
    public List<CartItem> Items { get; set; } = new();
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
}

public class CartItem {
    public string Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtTime { get; set; }
}
```

**API Endpoints:**
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/health` | Health check |
| POST | `/api/cart` | Create new cart |
| GET | `/api/cart/{cartId}` | Get cart contents |
| POST | `/api/cart/{cartId}/items` | Add item to cart |
| PUT | `/api/cart/{cartId}/items/{itemId}` | Update item quantity |
| DELETE | `/api/cart/{cartId}/items/{itemId}` | Remove item |
| GET | `/api/cart/{cartId}/total` | Get cart total |

**Dependencies:**
- `OpenTelemetry.Exporter.OpenTelemetryProtocol.Grpc`
- `OpenTelemetry.Instrumentation.AspNetCore`
- `System.Net.Http` (for CatalogService calls)

**Inter-Service Calls:**
- Calls CatalogService to validate products: `GET http://catalog-service:5001/api/products/{id}`
- Validates stock before adding item

**Telemetry:**
- Automatic HTTP server spans
- Custom spans for cart operations
- Outgoing spans for CatalogService calls (W3C Trace Context propagation)
- Metrics: cart size, operation counts
- Logs: structured with trace ID for correlation

---

### OrderService (Port 5003)

**Technology:** ASP.NET Core 6.0+  
**Runtime:** .NET CLR  
**Build System:** MSBuild / dotnet CLI  
**Role:** Order processing and fulfillment  

**Responsibilities:**
- Convert carts into orders
- Validate order before confirmation
- Maintain order history
- Report order status

**Data Model:**
```csharp
public class Order {
    public string Id { get; set; }  // Order ID
    public string CartId { get; set; }
    public string CustomerId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }  // Pending, Confirmed, etc.
    public DateTime CreatedAt { get; set; }
}

public class OrderItem {
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public enum OrderStatus {
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}
```

**API Endpoints:**
| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/health` | Health check |
| POST | `/api/orders` | Create order from cart |
| GET | `/api/orders/{orderId}` | Get order details |
| GET | `/api/orders/customer/{customerId}` | List customer orders |
| PUT | `/api/orders/{orderId}/status` | Update order status |

**Dependencies:**
- `OpenTelemetry.Exporter.OpenTelemetryProtocol.Grpc`
- `OpenTelemetry.Instrumentation.AspNetCore`
- `System.Net.Http` (for service calls)

**Inter-Service Calls:**
- Calls CartService: `GET http://cart-service:5002/api/cart/{cartId}`
- Calls CatalogService: `GET http://catalog-service:5001/api/products/{id}` for validation

**Telemetry:**
- Automatic HTTP server spans
- Custom spans for order validation and creation
- Outgoing spans for CartService and CatalogService calls
- Distributed trace context propagation (W3C Trace Context header)
- Metrics: orders created per minute, validation failures
- Logs: structured with trace ID for debugging

---

### Web Frontend (Port 3000)

**Technology:** Next.js 14+, React 18+, TypeScript  
**Runtime:** Node.js 18+  
**Build System:** npm/yarn, Next.js build pipeline  
**Role:** Customer-facing UI  

**Responsibilities:**
- Display product catalog
- Manage shopping cart
- Handle checkout flow
- Display order confirmation

**Key Pages:**
- `/` — Home / Product listing
- `/products` — Product catalog
- `/cart` — Shopping cart view
- `/checkout` — Order checkout
- `/orders` — Order history

**API Integration:**
```
Next.js frontend calls:
  • CatalogService: GET /api/products
  • CartService: POST /api/cart, GET /api/cart/{id}, PUT/DELETE operations
  • OrderService: POST /api/orders
```

**Observability Integration:**
- Instruments fetch calls to services with trace context
- Sends traces to OTel Collector at `http://otel-collector:4317`
- Includes browser-side metrics and logs

---

## Data Flow Examples

### Scenario 1: View Product List

```
1. Browser: GET http://localhost:3000
2. Next.js Frontend: Calls CatalogService
3. Frontend → CatalogService: GET http://catalog-service:5001/api/products
4. CatalogService: Returns [Product1, Product2, ...]
5. Frontend: Renders product list
6. Telemetry: Trace captured with spans for both frontend and CatalogService calls
```

### Scenario 2: Add Item to Cart

```
1. Browser: POST /api/cart/123/items {productId: 5, quantity: 2}
2. Frontend → CartService: POST http://cart-service:5002/api/cart/123/items
3. CartService: Receives request, extracts trace context header
4. CartService → CatalogService: GET http://catalog-service:5001/api/products/5 (validates)
5. CatalogService: Returns product info with trace context
6. CartService: Updates in-memory cart, calculates new total
7. CartService: Returns updated cart to frontend
8. Frontend: Displays updated cart
9. Telemetry: Single distributed trace spans frontend → CartService → CatalogService
   (All three use same Trace ID for correlation in Grafana)
```

### Scenario 3: Create Order

```
1. Browser: POST /api/orders {cartId: 123}
2. Frontend → OrderService: POST http://order-service:5003/api/orders
3. OrderService: Receives request, extracts trace context
4. OrderService → CartService: GET http://cart-service:5002/api/cart/123
5. CartService: Returns cart contents (with trace context propagated)
6. OrderService → CatalogService: GET http://catalog-service:5001/api/products/{each-item-id}
   (Validates each product, gets final prices)
7. CatalogService: Returns product info (traces propagated)
8. OrderService: Creates Order record, returns confirmation
9. Frontend: Displays order confirmation
10. Telemetry: Distributed trace shows entire call chain with latency at each step
    (visible in Tempo, logs searchable in Loki, metrics aggregated in Prometheus)
```

---

## Technology Decisions

| Layer | Technology | Rationale |
|-------|-----------|-----------|
| API Services | ASP.NET Core 6.0+ | Modern, high-performance, native OpenTelemetry support |
| Frontend | Next.js 14 | Full-stack TypeScript, built-in API routes, React ecosystem |
| Communication | REST/HTTP | Simplicity, debugging-friendly, standard for microservices |
| Observability | OpenTelemetry | Vendor-neutral, multi-language support, becoming industry standard |
| Metrics | Prometheus | Lightweight, pull-based model, excellent time-series storage |
| Logs | Loki | Log aggregation optimized for Grafana, label-based indexing |
| Traces | Tempo | Trace storage designed for Grafana integration, low overhead |
| Visualization | Grafana | Unified dashboards across metrics, logs, and traces |
| Deployment | Docker Compose | Local development simplicity, learning-appropriate |
| Data Storage | In-Memory | Learning project — no persistence needed, focuses on architecture |

---

## Deployment & Networking

**Deployment Target:** Docker Compose (local development)  
**Container Runtime:** Docker Engine in WSL 2  
**Networking:** Two isolated Docker networks via Docker bridge driver  

**Services in `minicart-app` network:**
- web (3000)
- catalog-service (5001)
- cart-service (5002)
- order-service (5003)

**Services in `minicart-observability` network:**
- otel-collector (4317/4318)
- prometheus (9090)
- loki (3100)
- tempo (3200)
- grafana (3000)

**Port Mappings (Host Access):**
- Frontend: 3000 → 3000 (accessible at http://localhost:3000)
- CatalogService: 5001 → 5001
- CartService: 5002 → 5002
- OrderService: 5003 → 5003
- Prometheus: 9090 → 9090 (observability only)
- Loki: 3100 → 3100 (observability only)
- Tempo: 3200 → 3200 (observability only)

---

## Observability Flow

**Telemetry Collection:**
1. Each service emits traces, metrics, logs via OpenTelemetry SDK
2. All telemetry flows to otel-collector:4317 (gRPC protocol)
3. OTel Collector processes and exports to:
   - Prometheus (for metrics)
   - Loki (for logs)
   - Tempo (for traces)
4. Grafana queries all three backends
5. Dashboards display correlated telemetry (same trace ID across metrics, logs, traces)

**Trace Context Propagation:**
- Uses W3C Trace Context standard
- Header: `traceparent: 00-<trace-id>-<span-id>-<flags>`
- Each service extracts and continues trace on outgoing calls
- Result: End-to-end visibility of request flow

---

## Scaling Considerations (Future)

**For Production:**
- Replace in-memory storage with persistent databases (PostgreSQL, Redis)
- Add API Gateway for routing and rate limiting
- Implement service-to-service authentication (mTLS)
- Move from Docker Compose to Kubernetes
- Add distributed caching layer
- Implement circuit breakers and retries
- Add API versioning strategy

**For Learning:**
- Current design is intentionally simplified for understanding
- Each component is independently runnable
- Architecture is extensible for adding features
