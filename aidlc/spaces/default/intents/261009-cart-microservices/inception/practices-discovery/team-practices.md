# Team Practices — MiniCart Microservices

**Date Affirmed:** 2026-10-11  
**Project:** MiniCart Learning Application  
**Scope:** ASP.NET Core microservices with distributed observability  

---

## Executive Summary

We are a learning-focused team building a three-service e-commerce system to demonstrate microservices architecture and observability. Our practices prioritize **clarity over cleverness**, **observability from day one**, and **fast feedback loops**. We treat tests and instrumentation as first-class artifacts, not afterthoughts. Every commit is a release candidate; every deployment is observable.

---

## Testing Strategy

### Philosophy
**Test-After Methodology** — We implement each service layer, then write tests to verify behavior. Tests are collaborative evidence that the service works as specified.

### Pyramid: Unit / Integration / E2E

| Layer | Ratio | Coverage | Tools | Purpose |
|-------|-------|----------|-------|---------|
| **Unit Tests** | 60% | 80% code coverage minimum | xUnit, Moq, FluentAssertions | Verify service business logic in isolation |
| **Integration Tests** | 30% | All inter-service calls | xUnit, HttpClient mock | Verify service-to-service communication works |
| **E2E Tests** | 10% | Happy path + error paths | Playwright (frontend), Docker containers | Verify complete user journeys work end-to-end |

### Test Coverage Targets

- **Unit Tests:** 80% line coverage minimum per service (enforced in CI)
- **Integration Tests:** 100% of inter-service HTTP calls must be tested
- **Contract Tests:** Each service producer must verify response format before consumer implements
- **Observability Tests:** Every request that crosses a service boundary must create a span; integration tests verify trace context propagation

### Test Execution

- **On Push:** Run unit tests locally (developer responsibility)
- **On PR:** Run full suite (unit + integration) in CI pipeline; block merge if coverage drops or tests fail
- **Pre-Merge:** Developer confirms local E2E smoke tests pass
- **On Merge to Main:** Run full suite again + publish docker images
- **Pre-Deploy:** Run E2E suite against staging environment

### Testing Tools

| Tool | Purpose | Scope |
|------|---------|-------|
| **xUnit** | Unit & integration test framework | All .NET services |
| **Moq** | Mocking HTTP clients and dependencies | Service isolation |
| **FluentAssertions** | Readable assertion syntax | Test readability |
| **Playwright** | Browser automation for E2E | Frontend flows only |
| **Docker Compose** | Local integration test environment | Run full stack locally |
| **OpenTelemetry Test SDK** | Verify trace emission | Observability tests |

### What Good Tests Look Like

✅ **Unit Test Example:**
```csharp
[Fact]
public void AddToCart_ValidProduct_IncrementQuantity()
{
    var cartService = new CartService(mockCatalogClient);
    var cart = cartService.AddItem("user1", 123, 1);
    
    Assert.NotNull(cart);
    Assert.Single(cart.Items);
    Assert.Equal(1, cart.Items[0].Quantity);
}
```

✅ **Integration Test Example:**
```csharp
[Fact]
public async Task AddToCart_CallsCatalogService_WithTraceContext()
{
    var handler = new MockHttpMessageHandler();
    var client = new HttpClient(handler);
    var service = new CartService(client);
    
    await service.AddItem("user1", 123, 1);
    
    Assert.Single(handler.Requests);
    var request = handler.Requests[0];
    Assert.True(request.Headers.Contains("traceparent"));
}
```

✅ **E2E Test Example:**
```csharp
[Fact]
public async Task UserJourney_ViewProducts_AddToCart_Checkout()
{
    using var browser = await Playwright.Firefox.LaunchAsync();
    var page = await browser.NewPageAsync();
    
    await page.GoToAsync("http://localhost:3000");
    await page.ClickAsync("text=Products");
    await page.ClickAsync("text=Add to Cart");
    
    var cartBadge = await page.GetAttributeAsync(".cart-badge", "data-count");
    Assert.Equal("1", cartBadge);
}
```

---

## CI/CD Pipeline

### Philosophy
**Every Commit is a Release Candidate** — If code passes all gates, it is deployable.

### Pipeline Stages

```
Push to Feature Branch
  ├─ [1] Lint & Format Check (< 1 min)
  │   └─ ESLint (frontend) + dotnet format (backend)
  │   └─ FAIL → Developer fixes locally
  │
  ├─ [2] Build (< 5 min)
  │   ├─ dotnet build (each service)
  │   └─ npm run build (frontend)
  │   └─ FAIL → Build error, cannot proceed
  │
  ├─ [3] Unit Tests (< 3 min)
  │   ├─ dotnet test (all unit tests)
  │   ├─ Coverage report (fail if < 80%)
  │   └─ FAIL → Test failure, cannot proceed
  │
  └─ [4] Security Scan (< 2 min)
      ├─ OWASP Dependency Check (NuGet + npm)
      ├─ Moderate vulnerabilities → Warning
      └─ Critical vulnerabilities → Block merge

Pull Request
  ├─ [5] Review Gate (manual)
  │   ├─ Tech lead approval required
  │   ├─ Check: Does code follow standards?
  │   └─ BLOCK → Address feedback
  │
  └─ [6] Integration Tests (< 5 min)
      ├─ Start Docker Compose stack
      ├─ Run integration test suite
      ├─ Verify trace context propagation
      └─ FAIL → Fix integration issue

Merge to Main
  ├─ [7] Full Test Re-Run (< 10 min)
  │   └─ Verify no race conditions
  │
  ├─ [8] Docker Image Build & Push (< 3 min per service)
  │   ├─ Build multi-stage Docker image
  │   ├─ Tag: sha256 of commit
  │   ├─ Push to local registry (dev environment)
  │   └─ FAIL → Cannot deploy if image build fails
  │
  └─ [9] Deployment to Staging (manual approval)
      ├─ Run docker-compose pull (new images)
      ├─ Execute smoke tests
      ├─ Verify services healthy
      └─ Staging environment live with latest code

Merge to Production Tag (manual, ceremony)
  └─ [10] Deploy to Production (manual)
      ├─ Same as staging
      ├─ Run production-specific tests
      └─ Monitor observability dashboards
```

### Artifact Storage

- **Docker Images:** Local Docker registry (learning environment)
  - Tag: `localhost:5000/minicart/<service>:<commit-sha>`
  - Retention: Keep last 10 builds per service
  - No DockerHub for learning (self-contained)

- **Test Reports:** Git commit (no separate artifact storage)
  - Coverage reports stored as PR comment
  - Failure reports sent to chat

- **Build Logs:** CI system retention (7 days)

### Gate Criteria

| Stage | Gate | Action on Failure |
|-------|------|-------------------|
| Lint | Pass/Fail | Developer fixes locally, re-push |
| Build | Pass/Fail | Stop pipeline, debug build error |
| Unit Tests | ≥ 80% coverage + all pass | Stop pipeline, developer writes tests |
| Security | No critical vulns | Block merge; security team reviews |
| Code Review | Tech lead approval | Code stays in PR; address feedback |
| Integration | All tests pass + traces verify | Fix inter-service issue or contract |
| Docker Build | Image successfully pushed | Skip deploy if build fails |
| Smoke Tests | All services healthy | Rollback or manual debug |

### What Developers Do Locally Before Pushing

1. ✅ Run `dotnet format` to format code
2. ✅ Run `dotnet build` to ensure compilation
3. ✅ Run `dotnet test` to verify unit tests pass
4. ✅ Run `docker-compose up` and test manually
5. ✅ Run integration test suite locally
6. ✅ Verify trace context in Grafana
7. ✅ Push feature branch (CI runs automatically)

---

## Resilience & Error Handling

### Philosophy
**Resilience is Not Optional** — Every service must assume other services might be slow, failing, or temporarily unavailable.

### Timeout Strategy

**Inter-Service Call Timeouts**
- **HTTP Request Timeout:** 2 seconds per call
- **Total Operation Timeout:** 5 seconds (allows 2-3 retries)
- **Configuration:** `appsettings.json` per environment
  ```json
  {
    "ServiceClients": {
      "CatalogServiceUrl": "http://catalog-service:5001",
      "TimeoutMs": 2000,
      "MaxRetries": 3
    }
  }
  ```

### Retry Strategy

**Exponential Backoff with Jitter**
- **Max Retries:** 3
- **Initial Delay:** 100ms
- **Growth Factor:** 2x (100ms → 200ms → 400ms)
- **Jitter:** ±10% randomization to avoid thundering herd
- **Idempotent Only:** Only retry GET requests; for POST/PUT, must track request ID

```csharp
// Example: CartService calling CatalogService
var policy = Policy
    .Handle<HttpRequestException>()
    .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(
            100 * Math.Pow(2, attempt - 1) + Random.Shared.Next(-10, 11)
        )
    );

var response = await policy.ExecuteAsync(() => 
    _httpClient.GetAsync($"{_catalogUrl}/api/products/{productId}")
);
```

### Circuit Breaker Pattern

**When to Open Circuit**
- **Threshold:** If 50% or more of requests fail over the last 10 requests
- **Open Duration:** 10 seconds (half-open state to test recovery)
- **Reset on Success:** When half-open request succeeds, close circuit

```csharp
var circuitPolicy = Policy
    .Handle<HttpRequestException>()
    .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,  // 50% of 10 requests
        durationOfBreak: TimeSpan.FromSeconds(10)
    );
```

### Fallback & Graceful Degradation

**Fallback Behavior by Service**
- **CatalogService Unavailable:** Return 503 Service Unavailable; do not proceed with order
- **CartService Unavailable:** Return 503 Service Unavailable
- **OrderService Unavailable:** Return 503 Service Unavailable

No stale-data fallbacks for learning environment (too complex); fail fast and clearly.

### Error Response Format

**Standard Error Response**
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "CATALOG_SERVICE_TIMEOUT",
    "message": "Catalog service did not respond in time. Please try again.",
    "traceId": "0af7651916cd43dd8448eb211c80319c"
  },
  "timestamp": "2026-10-11T10:30:00Z"
}
```

**Error Codes**
| Code | HTTP Status | Meaning |
|------|-------------|---------|
| `INVALID_REQUEST` | 400 | Malformed request |
| `NOT_FOUND` | 404 | Resource not found |
| `CATALOG_SERVICE_TIMEOUT` | 503 | Dependency unavailable |
| `CART_SERVICE_TIMEOUT` | 503 | Dependency unavailable |
| `INTERNAL_ERROR` | 500 | Unexpected server error |

---

## Observability Instrumentation

### Philosophy
**Observability is Built In** — Every request must be traceable, measurable, and loggable.

### Trace Strategy

**Sampling Rate**
- **Development:** 100% (capture every request for learning)
- **Production:** 10% (reduce noise, capture representative sample)
- **Critical Paths:** 100% sampling for checkout flow (business-critical)

**Trace Context Propagation**
- **Standard:** W3C Trace Context (traceparent, tracestate headers)
- **Requirement:** Every inter-service call MUST include traceparent header
- **Framework:** OpenTelemetry .NET SDK auto-instruments ASP.NET Core and HttpClient

**Spans Emitted Per Request**

```
User: View Products
  Root Span: HTTP GET /api/products (Web Frontend)
    └─ Child Span: Call to CatalogService (outgoing HTTP)
       └─ Child Span: HTTP GET /api/products (CatalogService)
          ├─ Child Span: In-memory collection query
          └─ Child Span: JSON serialization

User: Add to Cart (Complete Flow)
  Root Span: HTTP POST /api/cart (Web Frontend)
    └─ Child Span: Call to CartService (outgoing HTTP)
       └─ Child Span: HTTP POST /api/cart (CartService)
          ├─ Child Span: Call to CatalogService (outgoing HTTP)
          │  └─ Child Span: HTTP GET /api/products/{id} (CatalogService)
          │     └─ Child Span: In-memory lookup
          └─ Child Span: Cart state update
```

### Metrics Strategy

**Metrics Emitted Per Service**

| Metric | Type | Dimensions | Purpose |
|--------|------|-----------|---------|
| `http.server.request.duration` | Histogram | `http.method`, `http.status_code`, `http.route` | Track request latency per endpoint |
| `http.server.request.count` | Counter | `http.method`, `http.status_code` | Total request count by method/status |
| `http.client.request.duration` | Histogram | `http.method`, `peer.service` | Track outbound call latency per service |
| `process.runtime.go.goroutines` | Gauge | N/A | (Not applicable; .NET uses threads) |
| `process.memory.usage` | Gauge | `state` | Monitor memory consumption |

**Exemplars Linked to Traces**
- Every metric point includes optional link to a sample trace ID
- Users can click from metric spike → trace causation

### Logs Strategy

**Structured Logging Format**
```json
{
  "timestamp": "2026-10-11T10:30:00.123Z",
  "level": "INFO",
  "logger": "CartService",
  "message": "Added item to cart",
  "traceId": "0af7651916cd43dd8448eb211c80319c",
  "spanId": "b7ad6b7169203331",
  "userId": "user123",
  "productId": 456,
  "quantity": 1
}
```

**Log Levels Per Event**
| Level | When to Use | Example |
|-------|-------------|---------|
| `DEBUG` | Detailed diagnostic info | "Validating product stock: 5 available" |
| `INFO` | Notable business events | "Order #789 created successfully" |
| `WARN` | Recoverable issues | "Circuit breaker for CatalogService opened" |
| `ERROR` | Unrecoverable errors | "Failed to serialize order: NullReferenceException" |
| `FATAL` | System shutdown required | "Cannot connect to OTEL Collector" |

**PII Redaction**
- Do not log user passwords, payment info, or SSNs
- Log `userId` only as identifier
- Sanitize error messages before returning to client

### Dashboards (Grafana)

**Dashboard 1: User Journey Latency**
- Chart: Time from "Add to Cart" click → "Item in cart" confirmation
- Breakdown: Frontend latency, CartService latency, CatalogService latency
- Alert: If total latency > 2 seconds

**Dashboard 2: Service Health**
- Status: Each service's last health check (green/yellow/red)
- Error Rate: % of requests failing per service
- Request Latency: P50, P95, P99 per endpoint

**Dashboard 3: Observability Infrastructure**
- OTEL Collector: Traces/sec, metrics/sec, logs/sec
- Prometheus: Scrape success rate, storage used
- Loki: Log volume, query latency
- Tempo: Trace volume, query latency

**Dashboard 4: Trace Navigation (Tempo)**
- Search by trace ID or operation name
- View full span tree with timing
- Link from any span to logs and metrics

### Alerts

| Alert | Condition | Action |
|-------|-----------|--------|
| `ServiceUnavailable` | Any service responds 503 for > 1 min | Page on-call engineer |
| `HighErrorRate` | Error rate > 5% for > 5 min | Slack channel notification |
| `HighLatency` | P99 latency > 1 second for > 5 min | Slack channel notification |
| `DependencyTimeout` | Circuit breaker open for > 10 sec | Slack channel notification |

---

## Code Quality Standards

### Philosophy
**Clarity Over Cleverness** — Code should be readable first, optimized second.

### C# Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| **Classes & Structs** | PascalCase | `CartService`, `CartItem` |
| **Interfaces** | PascalCase with `I` prefix | `ICartService`, `ILogger` |
| **Methods & Properties** | PascalCase | `AddItem()`, `GetTotal()` |
| **Local Variables** | camelCase | `cartId`, `itemCount` |
| **Private Fields** | camelCase with `_` prefix | `_logger`, `_httpClient` |
| **Constants** | UPPER_SNAKE_CASE | `MAX_CART_SIZE`, `DEFAULT_TIMEOUT` |
| **Enum Values** | PascalCase | `OrderStatus.Confirmed` |

### Async/Await Patterns

✅ **Always Await**
```csharp
// Good: Async all the way through
public async Task<Cart> GetCartAsync(string userId)
{
    var cart = await _repository.GetAsync(userId);
    return cart;
}
```

❌ **Never Block Threads**
```csharp
// Bad: Deadlock-prone, blocks thread pool
public Cart GetCart(string userId)
{
    var cart = _repository.GetAsync(userId).Result;  // NEVER DO THIS
    return cart;
}
```

✅ **Async Methods Return Task or Task<T>**
```csharp
public async Task AddItemAsync(string userId, int productId)
{
    await _cartRepository.UpdateAsync(userId, productId);
}

public async Task<CartResponse> GetCartAsync(string userId)
{
    var cart = await _cartRepository.GetAsync(userId);
    return new CartResponse { Items = cart.Items };
}
```

### Dependency Injection

✅ **Constructor Injection (Always)**
```csharp
public class CartService
{
    private readonly ILogger<CartService> _logger;
    private readonly ICartRepository _repository;
    private readonly ICatalogClient _catalogClient;

    public CartService(
        ILogger<CartService> logger,
        ICartRepository repository,
        ICatalogClient catalogClient)
    {
        _logger = logger;
        _repository = repository;
        _catalogClient = catalogClient;
    }
}
```

❌ **Service Locator Pattern (Avoid)**
```csharp
// Bad: Hidden dependencies, hard to test
public class CartService
{
    public void AddItem(string userId, int productId)
    {
        var logger = ServiceLocator.GetService<ILogger>();  // AVOID
        var catalog = ServiceLocator.GetService<ICatalogClient>();
    }
}
```

### Error Handling

✅ **Catch Specific Exceptions**
```csharp
try
{
    var response = await _httpClient.GetAsync(url);
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Failed to call {Url}", url);
    throw new DependencyException("Catalog service unavailable", ex);
}
catch (OperationCanceledException ex)
{
    _logger.LogError(ex, "Request timeout after {Timeout}ms", timeout);
    throw new TimeoutException("Operation timed out", ex);
}
```

❌ **Catch-All Exceptions (Avoid)**
```csharp
try
{
    await SomeOperation();
}
catch (Exception ex)  // Too broad
{
    _logger.LogError(ex, "Something went wrong");
}
```

### Comments & Documentation

✅ **Public Methods Documented**
```csharp
/// <summary>
/// Adds an item to the user's shopping cart.
/// </summary>
/// <param name="userId">The user identifier</param>
/// <param name="productId">The product to add</param>
/// <returns>Updated cart contents</returns>
/// <exception cref="DependencyException">Thrown if catalog service is unavailable</exception>
public async Task<Cart> AddItemAsync(string userId, int productId)
{
    // Implementation...
}
```

✅ **Complex Logic Explained**
```csharp
// Exponential backoff retry: 100ms, 200ms, 400ms
// This prevents overwhelming a recovering service
var delay = (int)(100 * Math.Pow(2, attemptNumber - 1));
```

❌ **Obvious Comments (Avoid)**
```csharp
// Increment counter by 1
count++;
```

### Code Organization

**File Structure Per Service**
```
CartService/
├── Program.cs                          # Startup, DI registration
├── Controllers/
│   └── CartController.cs               # HTTP endpoints only
├── Services/
│   ├── CartService.cs                  # Business logic
│   └── CatalogServiceClient.cs         # External service calls
├── Models/
│   ├── Cart.cs                         # Entities
│   └── CartItem.cs
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs  # Request/response processing
├── Config/
│   └── OpenTelemetryConfig.cs          # Configuration
└── Tests/
    ├── CartServiceTests.cs             # Unit tests
    └── CartControllerTests.cs          # Integration tests
```

**Separation of Concerns**
- **Controllers:** HTTP binding, request validation, response formatting only
- **Services:** Business logic, dependencies, transactions
- **Models:** Data structures, validation rules
- **Middleware:** Cross-cutting concerns (auth, logging, error handling)
- **Config:** Settings and initialization

### Testing Considerations

✅ **Testable Code**
```csharp
// Inject dependency, easy to mock
public class CartService
{
    private readonly ICatalogClient _catalog;
    
    public CartService(ICatalogClient catalog)
    {
        _catalog = catalog;  // Can be mocked in tests
    }
}
```

❌ **Hard to Test**
```csharp
// Creates its own dependency, cannot mock
public class CartService
{
    public CartService()
    {
        _catalog = new HttpClient();  // Hardcoded, cannot mock
    }
}
```

---

## API Design & Versioning

### Philosophy
**APIs are Contracts** — Once published, an API must remain stable or provide a clear migration path.

### Versioning Strategy

**URL Path Versioning**
- Format: `/v<major>/api/<resource>`
- Example: `/v1/api/products`, `/v2/api/orders`
- Reason: Simple to trace, explicit in logs and dashboards

**Current Version:** v1 (all services)

### Response Format (Consistent Across All Endpoints)

**Success Response**
```json
{
  "success": true,
  "data": {
    "id": 123,
    "name": "Product Name",
    "price": 29.99
  },
  "error": null,
  "timestamp": "2026-10-11T10:30:00Z"
}
```

**Error Response**
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "INVALID_REQUEST",
    "message": "Product ID must be a positive integer",
    "traceId": "0af7651916cd43dd8448eb211c80319c"
  },
  "timestamp": "2026-10-11T10:30:00Z"
}
```

### HTTP Status Codes

| Code | Meaning | Use Case |
|------|---------|----------|
| 200 | OK | Successful GET or PUT |
| 201 | Created | Successful POST (resource created) |
| 400 | Bad Request | Malformed request (invalid JSON, missing fields) |
| 401 | Unauthorized | (Not used in learning project; no auth) |
| 404 | Not Found | Resource does not exist |
| 500 | Internal Server Error | Unexpected server error |
| 503 | Service Unavailable | Dependency unavailable (dependency timeout) |

### Deprecation Policy

When API changes are needed:

1. **Release v2 alongside v1** (both versions active)
2. **Announce deprecation:** Publish 6-month sunset date
3. **Include warning header:** All v1 responses include `Deprecation: true` header
4. **Provide migration guide:** Document changes from v1 → v2
5. **Remove old version:** After sunset, v1 returns 410 Gone

Example deprecation header:
```
Deprecation: true
Sunset: Sun, 11 Apr 2027 10:00:00 GMT
Link: </v2/api/products>; rel="successor-version"
```

### API Documentation

All endpoints documented with:
- **URL** and HTTP method
- **Request schema** (example JSON + field types)
- **Response schema** (success + error cases)
- **Status codes** returned
- **Trace context requirement** (traceparent header)
- **Example curl command**

Example:
```
GET /v1/api/products/{id}

Description: Retrieve a single product by ID

Request:
  URL: /v1/api/products/123
  Headers:
    traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01

Response (200 OK):
{
  "success": true,
  "data": {
    "id": 123,
    "name": "Widget",
    "price": 29.99,
    "stockLevel": 5
  },
  "error": null,
  "timestamp": "2026-10-11T10:30:00Z"
}

Response (404 Not Found):
{
  "success": false,
  "data": null,
  "error": {
    "code": "NOT_FOUND",
    "message": "Product with ID 999 not found",
    "traceId": "0af7651916cd43dd8448eb211c80319c"
  },
  "timestamp": "2026-10-11T10:30:00Z"
}
```

---

## Security Baseline

### Philosophy
**Simple Security for Learning** — No authentication (open APIs), but input validation and error handling.

### Input Validation

✅ **Validate All Input**
```csharp
[HttpPost("api/cart")]
public async Task<IActionResult> AddItem([FromBody] AddItemRequest request)
{
    // Validate required fields
    if (request?.ProductId <= 0)
        return BadRequest("Product ID must be positive");
    
    if (request.Quantity <= 0)
        return BadRequest("Quantity must be positive");
    
    // Proceed with business logic
    var cart = await _cartService.AddItemAsync(User.Id, request.ProductId);
    return Ok(cart);
}
```

❌ **Do Not Trust Client Input**
```csharp
// Bad: No validation
public async Task<IActionResult> AddItem(AddItemRequest request)
{
    var cart = await _cartService.AddItemAsync(
        request.UserId,      // User could pass any ID
        request.ProductId    // Could be negative
    );
    return Ok(cart);
}
```

### Error Message Handling

✅ **Don't Leak Stack Traces to Clients**
```csharp
try
{
    var order = await _orderService.CreateAsync(orderRequest);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Internal error creating order");
    return StatusCode(500, new {
        error = "An internal error occurred. Please try again later.",
        traceId = Activity.Current?.Id
    });
}
```

### Dependency Scanning

**NuGet Package Scanning**
- **Tool:** OWASP Dependency-Check (Maven plugin)
- **Frequency:** On every PR build
- **Gate:** Critical vulnerabilities block merge; moderate level warnings only

**npm Package Scanning** (Frontend)
- **Tool:** npm audit
- **Frequency:** On every PR build
- **Gate:** Critical vulnerabilities block merge; moderate level warnings only

### Secrets Management

**Development Environment**
- Secrets stored in `.env` file (git-ignored)
- Example: `.env.example` committed to show structure
- Loaded by docker-compose at runtime

**Production Environment**
- Secrets stored in environment variables (set by deployment system)
- Never commit real credentials to git
- Use AWS Secrets Manager or similar in production

Example:
```env
# .env.example (committed to git)
CATALOG_SERVICE_URL=http://catalog-service:5001
OTEL_COLLECTOR_URL=http://otel-collector:4317

# .env (git-ignored, actual secrets)
DATABASE_PASSWORD=actual_secret_here
```

### Code Review Requirements

- **Mandatory:** At least one tech lead approval before merge
- **Scope:** Review for security issues, not just functionality
- **Checklist:**
  - No hardcoded secrets
  - No unvalidated user input in queries
  - No exception details in API responses
  - No overly permissive error messages
  - Trace IDs included in errors (for debugging without exposing internals)

---

## Health Checks & Readiness

### Philosophy
**Observability is Mutual** — Services must report their health for orchestration and monitoring.

### Health Check Endpoints

**Endpoint:** `GET /health`

**Response (Healthy - 200 OK)**
```json
{
  "status": "healthy",
  "service": "CartService",
  "timestamp": "2026-10-11T10:30:00Z",
  "dependencies": {
    "catalog_service": "healthy",
    "otel_collector": "healthy"
  }
}
```

**Response (Degraded - 200 OK)**
```json
{
  "status": "degraded",
  "service": "CartService",
  "timestamp": "2026-10-11T10:30:00Z",
  "dependencies": {
    "catalog_service": "unhealthy",
    "otel_collector": "healthy"
  }
}
```

**Response (Unhealthy - 503 Service Unavailable)**
```json
{
  "status": "unhealthy",
  "service": "CartService",
  "timestamp": "2026-10-11T10:30:00Z",
  "dependencies": {
    "otel_collector": "unhealthy"
  }
}
```

### Readiness Probe

**Endpoint:** `GET /ready`

**Purpose:** Kubernetes readiness check (when deployed to K8s)

**Response (Ready - 200 OK)**
- Service can accept traffic
- All required dependencies available

**Response (Not Ready - 503 Service Unavailable)**
- Service still starting up
- Required dependencies not yet available

### Liveness Probe

**Endpoint:** `GET /live`

**Purpose:** Kubernetes liveness check (when deployed to K8s)

**Response (Alive - 200 OK)**
- Service process is running
- No deadlocks detected

**Response (Dead - 503 Service Unavailable)**
- Service process unresponsive
- Kubernetes will restart container

---

## Recommended Pre-Code-Generation Evidence

Before Code Generation begins, affirm these artifacts to ensure implementation aligns:

1. ✅ **Integration Test Plan**
   - Which inter-service calls are critical to test?
   - How to verify trace context propagation in tests?

2. ✅ **Observability Instrumentation Checklist**
   - Which spans to emit from each service?
   - Which custom metrics to track?
   - Dashboard queries to write?

3. ✅ **Resilience Test Scenarios**
   - How to simulate timeout? (use `Task.Delay()`)
   - How to verify circuit breaker opens?
   - How to test retry backoff?

4. ✅ **API Contract Examples**
   - Real example payloads for add-to-cart
   - Real example payloads for create-order
   - Error response examples

5. ✅ **CI/CD Script Templates**
   - dotnet build, test, publish commands
   - Docker build and push scripts
   - Health check verification scripts

---

## Sign-Off

**These practices are affirmed by the team and will govern Code Generation and Construction.**

- **Testing:** Test-after methodology, 80% coverage minimum, pyramid strategy
- **CI/CD:** Automated on push, quality gates before merge, Docker artifact storage
- **Resilience:** 2-second timeouts, 3 retries with exponential backoff, circuit breakers
- **Observability:** 100% sampling in dev, structured logging with trace context, Grafana dashboards
- **Code Quality:** C# conventions, async/await throughout, constructor injection
- **API Design:** URL versioning, consistent response format, W3C Trace Context headers
- **Security:** Input validation, no secrets in code, dependency scanning in CI

**Approved:** [Practices Discovery team consensus, 2026-10-11]
