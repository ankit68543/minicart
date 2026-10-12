# 🏗️ MiniCart Architecture & Network Design

## System Overview

MiniCart is a **three-tier microservices application** with **complete observability** using **Docker Compose** and **two separate networks**.

```
┌──────────────────────────────────────────────────────────┐
│                  CLIENT (Browser)                         │
└────────────────────┬─────────────────────────────────────┘
                     │ HTTP/HTTPS
         ┌───────────▼──────────────┐
         │    Next.js Frontend      │
         │    (Port 3000)           │
         │    - Product Listing     │
         │    - Shopping Cart UI    │
         │    - Checkout Flow       │
         └───────────┬──────────────┘
                     │
   ┌─────────────────┼─────────────────┐
   │                 │                 │
   ▼                 ▼                 ▼
┌─────────────┐  ┌──────────────┐  ┌──────────────┐
│  Catalog    │  │   Cart       │  │   Order      │
│  Service    │  │   Service    │  │   Service    │
│  :5001      │  │   :5002      │  │   :5003      │
│             │  │              │  │              │
│ GET /prod   │  │ POST /cart   │  │ POST /order  │
│ GET /prod/id│  │ GET /cart    │  │ GET /order   │
│             │  │ PUT /cart/:id│  │ PUT /order   │
│             │  │ DEL /cart/:id│  │              │
└────────┬────┘  └──────┬───────┘  └───────┬──────┘
         │              │                  │
         └──────────────┼──────────────────┘
                        │
                        │ OTLP (traces, metrics, logs)
                        ▼
        ┌───────────────────────────────────┐
        │  OpenTelemetry Collector          │
        │  :4317 (gRPC) / :4318 (HTTP)      │
        │  Receives & Routes Telemetry      │
        └────────────┬───────────┬──────────┘
                     │           │
         ┌───────────▼──┐   ┌──────▼────────┐
         │ Prometheus   │   │ Loki + Tempo  │
         │ :9090        │   │ :3100, :3200  │
         │ (Metrics)    │   │ (Logs, Traces)│
         └───────┬──────┘   └───────┬───────┘
                 │                  │
                 └──────────┬───────┘
                            │
                        ┌───▼───────┐
                        │  Grafana  │
                        │  :3000    │
                        │(Dashboards)
                        └───────────┘
```

---

## Docker Network Design

### **Network 1: `app` (Application Services)**

Services that form the core business logic:
- **Web** (Next.js frontend)
- **CatalogService** (.NET API)
- **CartService** (.NET API)
- **OrderService** (.NET API)

**Why this network?**
- Services on this network can communicate with each other
- Clean isolation: they don't need direct access to monitoring tools
- Enables Kubernetes-style architecture patterns

**DNS (via Docker Compose internal DNS):**
```
web:3000
catalog-service:5001
cart-service:5002
order-service:5003
```

### **Network 2: `observability` (Monitoring Stack)**

Services that collect, store, and visualize telemetry:
- **OpenTelemetry Collector**
- **Prometheus** (metrics storage)
- **Loki** (log storage)
- **Tempo** (trace storage)
- **Grafana** (visualization)

**Why this network?**
- Monitoring infrastructure is isolated
- Prevents accidental misconfiguration
- Easy to turn on/off without affecting applications
- Security: application services don't directly access prometheus, loki, tempo

**DNS (via Docker Compose):**
```
otel-collector:4317/4318
prometheus:9090
loki:3100
tempo:3200
grafana:3000
```

### **Cross-Network Communication**

Services from `app` network reach `observability` network **only** through the **OpenTelemetry Collector**:

```
┌─────────────┐
│   Service   │ (on app network)
│  (App API)  │
└────────┬────┘
         │ OTLP/gRPC to otel-collector
         │ (otel-collector is on both networks)
         ▼
┌─────────────────────────┐
│  OTel Collector         │ (on both app AND observability networks)
│  Connected to BOTH nets │
└────────┬────────────────┘
         │
         │ Exports to:
         ├─ Prometheus (on observability)
         ├─ Loki (on observability)
         └─ Tempo (on observability)
```

---

## Component Details

### **Next.js Web Frontend**

**Port:** 3000 (host accessible)  
**Network:** `app`  
**Responsibilities:**
- Display product catalog
- Manage shopping cart UI
- Checkout flow
- Order confirmation

**OpenTelemetry Integration:**
- Instruments API calls to microservices
- Sends traces via OTLP to otel-collector
- Structured logging with trace context

### **CatalogService (.NET Core)**

**Port:** 5001  
**Network:** `app`  
**Endpoints:**
- `GET /health` — Health check
- `GET /api/products` — List all products
- `GET /api/products/{id}` — Get product details

**In-Memory Data:**
```csharp
public class Product {
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
    public int Stock { get; set; }
}
```

**OpenTelemetry:**
- Automatic HTTP server spans
- Custom business metrics (e.g., products viewed)
- Structured JSON logs with trace context

### **CartService (.NET Core)**

**Port:** 5002  
**Network:** `app`  
**Endpoints:**
- `GET /health` — Health check
- `POST /api/cart` — Create/update cart
- `GET /api/cart/{cartId}` — Get cart contents
- `PUT /api/cart/{cartId}/items` — Add/update items
- `DELETE /api/cart/{cartId}/items/{productId}` — Remove item

**In-Memory Data:**
```csharp
public class Cart {
    public string Id { get; set; }
    public List<CartItem> Items { get; set; }
    public decimal Total { get; set; }
}

public class CartItem {
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtTime { get; set; }
}
```

**Inter-Service Communication:**
- Calls CatalogService to validate products and get pricing
- Uses HTTP client with OpenTelemetry instrumentation

### **OrderService (.NET Core)**

**Port:** 5003  
**Network:** `app`  
**Endpoints:**
- `GET /health` — Health check
- `POST /api/orders` — Create order from cart
- `GET /api/orders/{orderId}` — Get order details
- `GET /api/orders` — List user orders (optional filter)

**In-Memory Data:**
```csharp
public class Order {
    public string Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } // Pending, Confirmed, Shipped, etc.
    public List<OrderItem> Items { get; set; }
    public decimal Total { get; set; }
}
```

**Business Logic:**
- Validates cart contents
- Prevents overselling (checks stock)
- Creates order record
- Returns order confirmation

---

## Telemetry Flow

### **1. Traces (Distributed Tracing)**

A user creates an order:

```
[Browser] → [Web]
  └─ GET /api/products (trace_id: abc123)
  │   └─ calls [CatalogService] (span: get_products)
  │
  ├─ POST /api/cart (trace_id: abc123)
  │   └─ calls [CatalogService] (span: validate_products)
  │
  └─ POST /api/orders (trace_id: abc123)
      ├─ calls [CartService] (span: get_cart_details)
      └─ calls [OrderService] (span: create_order)

All spans tagged with: trace_id=abc123, service_name, span_name, status, duration
```

**Exported to:** OpenTelemetry Collector → Tempo  
**Viewed in:** Grafana → Tempo data source

### **2. Metrics (Prometheus)**

Each service emits:

```
# Request metrics
request_count_total{service="catalog", method="GET", status="200"} 1523
request_duration_seconds{service="catalog", endpoint="/products", le="0.1"} 1420

# Business metrics
products_viewed_total{product_id="101"} 345
cart_items_added_total{service="cart"} 892
orders_created_total{service="order"} 234

# Health metrics
service_up{service="catalog"} 1  (1=healthy, 0=down)
```

**Exported to:** OpenTelemetry Collector → Prometheus  
**Queried in:** Grafana → Prometheus data source (PromQL)

### **3. Logs (Loki)**

Structured JSON logs:

```json
{
  "timestamp": "2026-10-08T12:34:56Z",
  "level": "INFO",
  "service": "cart-service",
  "trace_id": "abc123",
  "span_id": "def456",
  "message": "Cart updated successfully",
  "user_id": "user123",
  "cart_id": "cart456",
  "item_count": 3,
  "total_price": 99.99
}
```

**Exported to:** OpenTelemetry Collector → Loki  
**Queried in:** Grafana → Loki data source (LogQL)

---

## Data Flow: One Complete Request

**User action:** Add item to cart

```
1. [Browser] Sends: POST /api/cart/items
                    Body: { productId: 101, quantity: 2 }
                    
2. [Web] Creates trace_id (or uses existing), adds span:
         span_name="add_to_cart"
         
3. [Web] Calls [CartService]: POST /api/cart/items
         Headers include: traceparent: 00-abc123-def456-01
         
4. [CartService] Receives request, extracts trace context
         Logs: "Received add_to_cart request"
         Calls [CatalogService]: GET /api/products/101
         
5. [CatalogService] Returns product info
         Emits metric: products_queried_total += 1
         
6. [CartService] Validates stock, updates cart
         Logs: "Cart updated: 2 units of product 101"
         Emits metric: cart_items_added_total += 1
         
7. [Web] Receives response, closes span
         Emits all spans/metrics/logs to OpenTelemetry Collector
         
8. [OTel Collector] Batches telemetry:
         Sends traces to Tempo
         Sends metrics to Prometheus
         Sends logs to Loki
         
9. [Grafana] Queries data sources:
         Shows trace timeline
         Shows metric spike in cart_items_added
         Shows correlated logs with trace_id
         
10. [User] Sees confirmation: "Item added (2 units)"
```

---

## Docker Compose Networks Definition

```yaml
# docker-compose.yml (simplified)

networks:
  app:
    driver: bridge
    name: minicart-app
    
  observability:
    driver: bridge
    name: minicart-observability

services:
  web:
    networks:
      - app
      
  catalog-service:
    networks:
      - app
      
  cart-service:
    networks:
      - app
      
  order-service:
    networks:
      - app
      
  otel-collector:
    networks:
      - app          # To receive from services
      - observability # To export to backends
      
  prometheus:
    networks:
      - observability
      
  loki:
    networks:
      - observability
      
  tempo:
    networks:
      - observability
      
  grafana:
    networks:
      - observability
```

---

## Service Health Checks

Each service has a health endpoint (`GET /health`):

```json
{
  "status": "healthy",
  "service": "catalog-service",
  "timestamp": "2026-10-08T12:34:56Z",
  "version": "1.0.0",
  "dependencies": {
    "database": "ok",
    "otel_collector": "ok"
  }
}
```

Docker Compose monitors these:

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:5001/health"]
  interval: 10s
  timeout: 5s
  retries: 3
  start_period: 5s
```

---

## Environment Variables

Services need:

```bash
# Catalog Service
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
ASPNETCORE_URLS=http://0.0.0.0:5001
SERVICE_NAME=catalog-service

# Cart Service
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
ASPNETCORE_URLS=http://0.0.0.0:5002
SERVICE_NAME=cart-service
CATALOG_SERVICE_URL=http://catalog-service:5001

# Order Service
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
ASPNETCORE_URLS=http://0.0.0.0:5003
SERVICE_NAME=order-service
CATALOG_SERVICE_URL=http://catalog-service:5001
CART_SERVICE_URL=http://cart-service:5002
```

---

## Key Architectural Decisions

| Decision | Rationale |
|----------|-----------|
| **In-memory data** | Simplicity; demonstrates concepts without database complexity |
| **No authentication** | Focus on observability and microservices patterns |
| **Two networks** | Teach network isolation and clean architecture |
| **OpenTelemetry** | Industry standard; works with any backend |
| **Docker Compose** | Simple orchestration; not Kubernetes (yet) |
| **Three services** | Enough for learning inter-service communication |

---

## Failure Scenarios (for Evaluation Phase)

We will demonstrate and troubleshoot:

1. **Service Down**: OrderService crashes → How to detect in metrics/logs/traces
2. **Slow Request**: CatalogService responds slowly → How latency appears in metrics
3. **Invalid Checkout**: Overselling scenario → How errors appear in traces
4. **Missing Telemetry**: OTel Collector unreachable → How to detect configuration issues

Each scenario includes recovery procedures and observability insights.
