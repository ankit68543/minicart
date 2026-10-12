# Observability & Telemetry Guide

This guide explains how to instrument services with OpenTelemetry, configure the observability stack, and investigate issues across traces, metrics, and logs.

---

## OpenTelemetry Concepts

### Three Pillars of Observability

**1. Traces** — Request journey across services
- Shows a single user request flowing through multiple services
- Includes timing, errors, and dependencies
- Example: User clicks "Add to Cart" → request travels through web → cart-service → catalog-service

**2. Metrics** — Aggregated measurements over time
- Request rate, latency, error rate
- Business metrics (orders/minute, cart size)
- System metrics (CPU, memory, network)

**3. Logs** — Detailed event records
- Structured JSON logs with context
- Tied to trace IDs for correlation
- Example: "Added item 2 to cart, quantity 1"

### How They Connect

```
Trace: "user adds item to cart"
  ├─ Span 1: web → cart-service (5ms)
  ├─ Span 2: cart-service → catalog-service (10ms)
  └─ Span 3: order validation (8ms)

Metrics: http_request_duration_seconds = 23ms (aggregate)

Logs: [TRACE_ID] Added item, validated stock, updated cart
```

Click on a trace in Grafana to see the associated logs.

---

## W3C Trace Context

All services propagate trace IDs using **W3C Trace Context** standard:

```
traceparent: 00-550e8400e29b41d4a716446655440000-a7d1d1c0e29b41d4-01
```

| Part | Value | Meaning |
|------|-------|---------|
| `00` | Version | W3C Trace Context v0 |
| `550e8400...` | Trace ID | Unique request ID (128-bit) |
| `a7d1d1c0...` | Span ID | Current span ID (64-bit) |
| `01` | Flags | Sampled (1 = yes, 0 = no) |

**Key point:** All OpenTelemetry libraries automatically propagate this header. You just need to read it.

---

## OpenTelemetry Configuration

### Environment Variables (All Services)

Set on every service container:

```env
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_SERVICE_NAME=service-name
OTEL_SDK_DISABLED=false
OTEL_TRACES_SAMPLER=always_on
OTEL_RESOURCE_ATTRIBUTES=environment=development,service.version=1.0.0
```

| Variable | Value | Meaning |
|----------|-------|---------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://otel-collector:4317` | Where to send telemetry (gRPC) |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` | Use gRPC (faster, compressed) |
| `OTEL_SERVICE_NAME` | `cart-service` | How this service identifies itself |
| `OTEL_TRACES_SAMPLER` | `always_on` | Capture 100% of traces (for learning) |
| `OTEL_RESOURCE_ATTRIBUTES` | `key=value,...` | Additional metadata |

---

## Instrumentation by Service

### 1. Next.js Frontend

**Libraries:**
```json
{
  "@opentelemetry/api": "^1.7.0",
  "@opentelemetry/sdk-node": "^0.43.1",
  "@opentelemetry/sdk-trace-node": "^0.43.1",
  "@opentelemetry/auto-instrumentations-node": "^0.39.4",
  "@opentelemetry/exporter-metrics-prometheus": "^0.43.0",
  "@opentelemetry/exporter-trace-otlp-grpc": "^0.43.1"
}
```

**Server-Side Tracing (Next.js API routes):**

```typescript
// lib/otel.ts
import { NodeSDK } from "@opentelemetry/sdk-node";
import { getNodeAutoInstrumentations } from "@opentelemetry/auto-instrumentations-node";
import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-grpc";

const sdk = new NodeSDK({
  traceExporter: new OTLPTraceExporter({
    url: process.env.OTEL_EXPORTER_OTLP_ENDPOINT || "http://localhost:4317",
  }),
  instrumentations: [getNodeAutoInstrumentations()],
});

sdk.start();
process.on("SIGTERM", () => sdk.shutdown());
```

**Browser-Side Tracing (Optional):**

Capture user interactions and navigation using `@opentelemetry/sdk-web`.

### 2. ASP.NET Core Services

**NuGet Packages:**
```xml
<ItemGroup>
  <PackageReference Include="OpenTelemetry" Version="1.6.0" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.6.0" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.6.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.5.1-beta1" />
  <PackageReference Include="OpenTelemetry.Instrumentation.HttpClient" Version="1.5.1-beta1" />
</ItemGroup>
```

**Startup Configuration (Program.cs):**

```csharp
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(
                Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") 
                ?? "http://localhost:4317"
            );
        }));

var app = builder.Build();

// Configure middleware
app.UseRouting();
app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
app.Run();
```

**Manual Instrumentation (Custom Spans):**

```csharp
// In a controller or service
using System.Diagnostics;

var activity = new Activity("ValidateProduct").Start();
try
{
    var product = await _catalogService.GetProduct(id);
    activity?.SetTag("product.id", id);
    activity?.SetTag("product.exists", product != null);
    // ... validation logic
}
finally
{
    activity?.Dispose();
}
```

---

## Prometheus Configuration

**File:** `observability/prometheus/prometheus.yml`

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: "otel-collector"
    static_configs:
      - targets: ["otel-collector:8888"]  # OTel Prometheus exporter

  - job_name: "catalog-service"
    static_configs:
      - targets: ["catalog-service:9464"]  # .NET Prometheus exporter

  - job_name: "cart-service"
    static_configs:
      - targets: ["cart-service:9464"]

  - job_name: "order-service"
    static_configs:
      - targets: ["order-service:9464"]
```

**How it works:**
1. Prometheus scrapes metrics endpoints every 15 seconds
2. Stores time-series data in `/prometheus` volume
3. Queries stored via PromQL language
4. Grafana queries Prometheus for visualization

---

## Loki Configuration

**File:** `observability/loki/loki-config.yml`

```yaml
auth_enabled: false

ingester:
  chunk_idle_period: 3m
  chunk_retain_period: 1m
  max_chunk_age: 1h

limits_config:
  enforce_metric_name: false
  reject_old_samples: true
  reject_old_samples_max_age: 168h

schema_config:
  configs:
    - from: 2020-10-24
      store: boltdb-shipper
      object_store: filesystem
      schema:
        version: v11
        index:
          prefix: index_
          period: 24h

server:
  http_listen_port: 3100
  log_level: info
```

**How to collect logs from containers:**

Use Docker's Loki logging driver in `docker-compose.yml`:

```yaml
services:
  cart-service:
    logging:
      driver: loki
      options:
        loki-url: "http://localhost:3100/loki/api/v1/push"
        loki-external-url: "http://loki:3100"
        loki-retries: "5"
        loki-batch-size: "400"
```

Or use Promtail to tail JSON logs files.

---

## Tempo Configuration

**File:** `observability/tempo/tempo.yml`

```yaml
server:
  http_listen_port: 3200

distributor:
  receivers:
    otlp:
      protocols:
        grpc:
          endpoint: 0.0.0.0:4317
        http:
          endpoint: 0.0.0.0:4318

storage:
  trace:
    backend: local
    local:
      path: /var/tempo/traces

metrics_generator:
  processor:
    service_graphs:
      enabled: true
```

**How traces flow:**
1. App sends trace via OTLP to Tempo
2. Tempo stores spans in local storage
3. Grafana queries Tempo by trace ID
4. Tempo can show service graph (dependencies)

---

## Grafana Data Sources & Dashboards

### Data Sources

Configure in Grafana UI or via provisioning:

**Prometheus:**
```yaml
apiVersion: 1
datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
```

**Loki:**
```yaml
  - name: Loki
    type: loki
    access: proxy
    url: http://loki:3100
```

**Tempo:**
```yaml
  - name: Tempo
    type: tempo
    access: proxy
    url: http://tempo:3200
```

### Example Dashboard Panels

**Panel 1: Request Rate (Graph)**
```promql
rate(http_requests_total[5m])
```
Shows requests per second by service.

**Panel 2: Request Duration (Heatmap)**
```promql
rate(http_request_duration_seconds_bucket[5m])
```
Shows latency distribution.

**Panel 3: Error Rate (Gauge)**
```promql
sum(rate(http_requests_total{status=~"5.."}[5m])) / sum(rate(http_requests_total[5m]))
```
Shows error percentage.

**Panel 4: Logs (Table)**
```logql
{service="cart-service"} | json | level="ERROR"
```
Shows error logs from cart-service.

---

## Querying Observability Data

### PromQL Examples

**1. Request rate by service:**
```promql
rate(http_requests_total[5m])
```

**2. 95th percentile latency:**
```promql
histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m]))
```

**3. Error rate:**
```promql
rate(http_requests_total{status=~"5.."}[5m])
```

**4. Requests by method and route:**
```promql
sum by (method, route) (rate(http_requests_total[5m]))
```

### LogQL Examples

**1. Logs from a specific service:**
```logql
{service="cart-service"}
```

**2. Error logs with trace context:**
```logql
{level="ERROR"} | json | trace_id != ""
```

**3. Logs containing "added item":**
```logql
{service="cart-service"} |= "added item"
```

**4. Parse JSON and filter:**
```logql
{service="cart-service"} | json | action="add_item" | product_id > 5
```

### Trace Navigation

**In Grafana:**
1. Open Tempo data source
2. Click "Search" tab
3. Filter by:
   - Service name
   - Duration range
   - Status (OK, Error)
4. Click a trace to see full span hierarchy
5. Click a span to see associated logs (if trace ID matches)

---

## Implementing End-to-End Observability

### Step 1: Configure Services

Set `OTEL_EXPORTER_OTLP_ENDPOINT` on each service pointing to OTel Collector.

### Step 2: Instrument Code

Add OpenTelemetry libraries and initialize SDK in each service.

### Step 3: Test Observability

```bash
# 1. Start services
docker compose up -d

# 2. Make a request
curl http://localhost:3000/api/cart

# 3. Check Prometheus
curl http://localhost:9090/api/v1/query?query=http_requests_total

# 4. Check Loki
curl 'http://localhost:3100/loki/api/v1/query?query={service="cart-service"}'

# 5. Check Tempo
curl http://localhost:3200/api/traces/<trace_id>

# 6. Open Grafana
# http://localhost:3000
# Admin / admin
# Explore Prometheus/Loki/Tempo data sources
```

### Step 4: Investigate a Request

**Scenario:** User reports slow checkout

**Investigation Path:**
1. **Open Grafana** → Prometheus → Query `rate(http_request_duration_seconds_bucket[5m])` → See latency spike at 15:30
2. **Open Grafana** → Tempo → Search for traces with duration > 1s, created at 15:30
3. **Find a slow trace** → See which span is slow (e.g., OrderService validation)
4. **Click that span** → See its trace ID (e.g., `550e8400...`)
5. **Open Grafana** → Loki → Query `{trace_id="550e8400..."}`
6. **See detailed logs** → OrderService was calling CatalogService, which timed out
7. **Fix:** Retry logic or timeout adjustment
8. **Verify:** Re-run same query, see latency normalized

---

## Common Queries & Use Cases

### Use Case: "Why is CartService slow?"

```promql
# Check latency
rate(http_request_duration_seconds_bucket{service="cart-service", le="1"}[5m])

# Check error rate
rate(http_requests_total{service="cart-service", status="500"}[5m])

# Check dependencies
# Look at Tempo trace to see which service CartService calls
```

### Use Case: "Which requests are failing?"

```logql
{level="ERROR"} | json | status="500"
```

Then click on a log entry → see trace ID → open in Tempo.

### Use Case: "What's the busiest service?"

```promql
sum by (service) (rate(http_requests_total[5m]))
```

### Use Case: "Are there any slow dependencies?"

```promql
# In Tempo: Service Graph shows dependencies
# Slow edges = slow downstream calls
```

---

## Failure Simulation (Testing Observability)

### Test 1: Inject Latency

Add delay to CartService:

```csharp
// In controller
await Task.Delay(2000);  // 2 second delay
```

**Observe:**
- Prometheus: latency spike
- Tempo: span duration increased
- Logs: timestamps show delay
- Grafana: dashboard alerts

**Recovery:** Remove delay, rebuild image.

### Test 2: Simulate Error

Throw exception in OrderService:

```csharp
if (order.Items.Count == 0)
    throw new InvalidOperationException("Cart is empty");
```

**Observe:**
- Metrics: error_count incremented
- Logs: exception message captured
- Traces: span status = ERROR
- Grafana: error rate spiked

**Recovery:** Fix validation logic.

### Test 3: Service Unavailable

Stop a service:

```bash
docker compose stop catalog-service
```

**Observe:**
- Web gets connection errors
- Trace shows failed HTTP call
- Logs show connection refused
- Metrics show error rate > 0

**Recovery:** Restart service.

---

## Best Practices

1. ✅ **Use consistent trace ID format** (W3C standard)
2. ✅ **Add trace ID to logs** via context propagation
3. ✅ **Avoid high-cardinality labels** (e.g., user IDs, specific values)
4. ✅ **Never log sensitive data** (passwords, tokens, credit cards)
5. ✅ **Set appropriate sample rates** (100% for learning, lower for high-volume production)
6. ✅ **Use structured JSON logs** (not free text)
7. ✅ **Tag spans with business context** (user_id, product_id, order_id)
8. ✅ **Monitor the observers** (disk space for Prometheus, retention for Loki)

---

## Next Steps

- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) — Common issues
- Run `/aidlc` to proceed with Phase 1 (IDEATION)
