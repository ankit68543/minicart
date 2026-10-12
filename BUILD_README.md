# MiniCart: Microservices Shopping Application with OpenTelemetry Observability

A learning project demonstrating three microservices (CatalogService, CartService, OrderService) with complete end-to-end observability using OpenTelemetry, Prometheus, Loki, and Tempo.

## 📋 Architecture

### Services
- **CatalogService** (Port 5001): Product catalog management
  - GET `/v1/catalog/products` - List all products
  - GET `/v1/catalog/products/{id}` - Get product details

- **CartService** (Port 5002): Shopping cart management
  - POST `/v1/cart` - Create new cart
  - POST `/v1/cart/{cartId}/items` - Add item to cart
  - DELETE `/v1/cart/{cartId}/items/{itemId}` - Remove item from cart
  - GET `/v1/cart/{cartId}` - Get cart contents

- **OrderService** (Port 5003): Order processing
  - POST `/v1/orders` - Create order from cart
  - GET `/v1/orders/{orderId}` - Get order details

- **Next.js Frontend** (Port 3000): React-based shopping interface

### Observability Stack
- **OTel Collector** (Ports 4317/4318): Receives traces, metrics, logs
- **Prometheus** (Port 9090): Metrics storage and querying
- **Loki** (Port 3100): Logs aggregation
- **Tempo** (Port 3200): Distributed traces
- **Grafana** (Port 3000): Dashboards and visualization

## 🚀 Quick Start

### Prerequisites
- Docker and Docker Compose
- .NET 5.0 SDK (for local development)
- Node.js 18+ (for frontend development)

### Run with Docker Compose

```bash
# Navigate to project root
cd d:\OTelLearning\minicart

# Build and start all services
docker-compose up -d

# Wait for services to start (~30 seconds)
docker-compose ps

# Access the application
# Frontend: http://localhost:3000
# Catalog API: http://localhost:5001/v1/catalog/products
# Cart API: http://localhost:5002/v1/cart
# Order API: http://localhost:5003/v1/orders
# Grafana: http://localhost:3000 (admin/admin)
```

### View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f catalog-service
docker-compose logs -f cart-service
docker-compose logs -f order-service
```

### Stop Services
```bash
docker-compose down

# Remove volumes too
docker-compose down -v
```

## 📊 Observability

### Tracing (Tempo)
- View distributed traces showing cart-to-catalog calls
- Access via Grafana → Explore → Tempo

### Metrics (Prometheus)
- HTTP request duration (p50, p95, p99)
- Request count by endpoint
- Error rates per service

### Logs (Loki)
- Structured logs with trace IDs
- Query by service, trace ID, or time range

### Dashboards
View pre-built dashboards in Grafana:
1. **Service Health**: Uptime, latency, error rates
2. **User Journey**: End-to-end cart → order latency
3. **Microservices Map**: Service dependencies and call volumes

## 🧪 Testing the Application

### Test via Frontend
1. Open http://localhost:3000
2. Browse products in left column
3. Click "Add to Cart" on any product
4. Click "Checkout" to create order

### Test via cURL

```bash
# 1. Create a cart
CART_ID=$(curl -s -X POST http://localhost:5002/v1/cart | jq -r '.data.id')

# 2. View catalog
curl -s http://localhost:5001/v1/catalog/products | jq

# 3. Add product to cart (product_id=1)
curl -s -X POST "http://localhost:5002/v1/cart/$CART_ID/items?productId=1&quantity=2" | jq

# 4. View cart
curl -s http://localhost:5002/v1/cart/$CART_ID | jq

# 5. Create order
ORDER_ID=$(curl -s -X POST http://localhost:5003/v1/orders \
  -H "Content-Type: application/json" \
  -d "{\"cartId\": \"$CART_ID\"}" | jq -r '.data.id')

# 6. View order
curl -s http://localhost:5003/v1/orders/$ORDER_ID | jq
```

## 🏗️ Development

### Local Development (Without Docker)

#### CatalogService
```bash
cd apps/services/CatalogService
dotnet restore
dotnet run
# Runs on http://localhost:5001
```

#### CartService
```bash
cd apps/services/CartService
dotnet restore
dotnet run
# Runs on http://localhost:5002
# Requires CatalogService running
```

#### OrderService
```bash
cd apps/services/OrderService
dotnet restore
dotnet run
# Runs on http://localhost:5003
# Requires CartService running
```

#### Frontend
```bash
cd apps/web
npm install
npm run dev
# Runs on http://localhost:3000
```

## 📚 Learning Outcomes

This project teaches:
- **Microservices Communication**: REST APIs with W3C Trace Context headers
- **Distributed Tracing**: OpenTelemetry span propagation across services
- **Resilience Patterns**: Timeouts (2s), retries (3x), circuit breaker (50% threshold)
- **Observability**: Traces, metrics, logs, and dashboards
- **Docker & Containerization**: Multi-container application orchestration
- **API Design**: Consistent response formats, versioning, error handling

## 🔧 Configuration

### Environment Variables

**Services:**
```
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
SERVICE_NAME=catalog-service
ASPNETCORE_URLS=http://0.0.0.0:5001
```

**Frontend:**
```
NEXT_PUBLIC_CATALOG_URL=http://localhost:5001
NEXT_PUBLIC_CART_URL=http://localhost:5002
NEXT_PUBLIC_ORDER_URL=http://localhost:5003
```

## 📁 Project Structure

```
minicart/
├── apps/
│   ├── services/
│   │   ├── CatalogService/      # Product catalog
│   │   ├── CartService/          # Shopping cart
│   │   └── OrderService/         # Order processing
│   └── web/                       # Next.js frontend
├── observability/
│   ├── otel-collector/           # OpenTelemetry Collector config
│   ├── prometheus/               # Prometheus config
│   ├── loki/                     # Loki config
│   ├── tempo/                    # Tempo config
│   └── grafana/                  # Grafana dashboards
├── docker-compose.yml            # Orchestration
└── README.md                      # This file
```

## ⚠️ Troubleshooting

### Services won't start
```bash
# Check logs
docker-compose logs catalog-service

# Common issues:
# - Port 5001-5003 already in use
# - Not enough disk space
# - Docker daemon not running
```

### Frontend can't connect to APIs
- Ensure services are running: `docker-compose ps`
- Check CORS settings in service code
- Verify API URLs in frontend .env

### Observability stack not receiving data
- Check OTel Collector logs: `docker-compose logs otel-collector`
- Verify OTEL_EXPORTER_OTLP_ENDPOINT is set correctly
- Check firewall/network isolation

### High latency in traces
- Verify 2-second timeout setting in service code
- Check database/I/O performance
- Monitor CPU and memory usage

## 📖 Further Reading

- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
- [W3C Trace Context](https://www.w3.org/TR/trace-context/)
- [Microservices Patterns](https://microservices.io/patterns/index.html)
- [Observability Engineering](https://www.oreilly.com/library/view/observability-engineering/9781492076438/)

## 📝 License

This project is for educational purposes.
