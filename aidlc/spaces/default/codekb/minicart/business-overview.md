# Business Overview — MiniCart Microservices

**Project:** MiniCart Learning Application  
**Type:** E-commerce / Shopping Cart System  
**Scope:** Three-service microservices architecture with observability

---

## What Does This System Do?

MiniCart is a **simplified e-commerce platform** that enables customers to:

1. **Browse Products** — Discover items in a digital catalog
2. **Manage Shopping Cart** — Add, update, remove items in a personal cart
3. **Process Orders** — Convert cart contents into confirmed orders

The system is designed as a **learning project** to teach:
- Microservices architecture patterns (service independence, inter-service communication)
- Modern observability practices (distributed tracing, metrics, logs correlation)
- Container orchestration with Docker Compose
- AIDLC workflow for structured development

---

## How Do the 3 Services Fit Together?

### Service Ecosystem

```
┌─────────────────────────────────────────────────────────┐
│                   End User (Browser)                    │
└──────────────────────────┬──────────────────────────────┘
                           │
                    ┌──────▼──────┐
                    │  Web UI     │
                    │  (Next.js)  │
                    │  Port 3000  │
                    └──┬──────┬───┘
              ┌────────┘      └─────────┐
              │                         │
        ┌─────▼────────┐        ┌──────▼──────┐
        │  Catalog     │        │   Cart      │
        │  Service     │        │   Service   │
        │  Port 5001   │        │   Port 5002 │
        └──────────────┘        └──────┬──────┘
                                       │
                                  ┌────▼──────┐
                                  │   Order   │
                                  │  Service  │
                                  │ Port 5003 │
                                  └───────────┘
```

### Service Responsibilities

| Service | Role | Key Functions |
|---------|------|---------------|
| **CatalogService** | Product Repository | Maintains product database, provides product listings, returns product details and pricing |
| **CartService** | Cart Management | Manages user shopping carts, validates product availability, calculates cart totals |
| **OrderService** | Order Processing | Converts carts into orders, confirms orders, maintains order history |

### Cross-Service Communication

- **CartService → CatalogService**: When a customer adds a product, CartService calls CatalogService to verify the product exists, get current pricing, and check stock availability
- **OrderService → CartService**: When creating an order, OrderService retrieves full cart details
- **OrderService → CatalogService**: OrderService validates product availability before confirming orders

---

## Core Business Functions

### 1. Product Discovery

**Flow:**
1. User navigates to Web UI
2. Web UI queries CatalogService for product list
3. CatalogService returns: Product ID, Name, Price, Description, Stock Level

**Business Value:** Users can browse available items before purchasing

### 2. Cart Management

**Flow:**
1. User adds product to cart
2. Web UI posts to CartService
3. CartService validates product exists (queries CatalogService)
4. CartService stores item in customer's cart
5. CartService returns updated cart summary

**Business Value:** Users can accumulate items, change quantities, and prepare for checkout

### 3. Order Checkout

**Flow:**
1. User submits order from Web UI
2. Web UI calls OrderService with cart contents
3. OrderService retrieves full cart from CartService
4. OrderService validates each item with CatalogService
5. OrderService creates order record with order ID and timestamp
6. OrderService returns confirmation

**Business Value:** Customers finalize purchases with confirmation

---

## System Boundaries

### What MiniCart Includes

✅ Product catalog management  
✅ Shopping cart state (in-memory)  
✅ Order creation and tracking  
✅ Complete distributed tracing and observability  
✅ Docker-based deployment  
✅ API-first design with standardized contracts  

### What MiniCart Does NOT (by Design)

❌ Payment processing (no payment gateway integration)  
❌ Persistent database (in-memory only, for learning)  
❌ Authentication/authorization (open API for simplicity)  
❌ Inventory management (no stock reduction on order — simplified)  
❌ Shipping integration (learning focus, not production logistics)  

---

## Target Users / Personas

1. **Software Engineering Students** — Learn microservices patterns through hands-on implementation
2. **DevOps/SRE Learners** — Understand observability, logging, and distributed tracing
3. **Architecture Teams** — Reference implementation for AIDLC workflow and service design
4. **OpenTelemetry Users** — Practical examples of instrumentation across multiple services

---

## Business Metrics to Track (via Observability)

- **Catalog Queries** — How often are products viewed?
- **Cart Operations** — How many add/remove operations per user session?
- **Order Rate** — Orders created per minute (business throughput)
- **Order Latency** — How long does checkout take?
- **Inter-Service Latency** — How fast are service-to-service calls?
- **Error Rate** — What % of requests fail?
- **Stock Validation Failures** — How often do out-of-stock errors occur?

These metrics are captured automatically via OpenTelemetry instrumentation.

---

## Success Criteria (Learning Outcomes)

When the system is complete and operational:

✅ All three services are independently deployable and running  
✅ End-to-end request flows are visible in distributed traces  
✅ Metrics, logs, and traces are correlated (same trace ID appears in all three)  
✅ Grafana dashboards show service health and request patterns  
✅ AIDLC workflow phases have been executed with documented decisions  
✅ Code is testable and follows microservice patterns  
