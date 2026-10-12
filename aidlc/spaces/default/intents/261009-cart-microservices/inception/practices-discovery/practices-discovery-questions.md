# Practices Discovery Summary Confirmation

## Review & Affirm Team Practices for MiniCart

Your team has discovered and affirmed comprehensive practices for this microservices project. Review the summary below and confirm these practices guide Code Generation.

---

## Testing Strategy

**Test Pyramid & Coverage:**
- 60% Unit Tests: Controllers, Services, Models with mocked dependencies
- 30% Integration Tests: Inter-service calls (CartService ↔ CatalogService), trace context propagation
- 10% End-to-End Tests: Complete user journeys (View → Add → Checkout)
- **Coverage Target:** 80% minimum per service
- **Tools:** xUnit for unit/integration, Playwright for E2E

**Observability Testing (Required):**
- Every integration test MUST verify trace context (traceparent, tracestate headers)
- Resilience testing: Verify 2-second timeouts, 3 retries with backoff, circuit breaker behavior

**CI Integration:**
- Tests run on every push
- Coverage regression: Fail if coverage drops > 2%
- SonarQube integration for trend tracking

---

## CI/CD Pipeline (10 Automated Stages)

1. **Code Lint** — C# style checks
2. **Build** — Compile each service
3. **Unit Tests** — Run with coverage report
4. **Integration Tests** — Cross-service validation
5. **Security Scan** — Dependency scanning (Snyk)
6. **SonarQube** — Code quality analysis
7. **Docker Build** — Build container images
8. **Code Review Gate** — Mandatory approval on PR
9. **Integration Environment** — Auto-deploy to staging
10. **Artifact Storage** — Push to registry

**Merge Gate:** All 10 stages pass + 1 code review approval required

---

## Resilience & Error Handling

**Timeout Strategy:**
- CartService → CatalogService: 2 seconds (industry standard for microservices)
- OrderService → CartService: 2 seconds

**Retry Logic:**
- Retries: 3 attempts
- Backoff: Exponential (100ms, 200ms, 400ms)
- Retryable errors: 408 (timeout), 429 (rate limit), 5xx (server error)
- Non-retryable: 400 (bad request), 401 (unauthorized), 404 (not found)

**Circuit Breaker:**
- Failure threshold: 50% of requests fail
- Window: 10-second observation window
- States: Closed (normal) → Open (failing) → Half-Open (testing recovery)

**Fallback:** Return structured error response (no silent failures)

**API Response Format (All Responses):**
```json
{
  "success": true/false,
  "data": { /* business data */ },
  "error": "Human-readable error message",
  "traceId": "00-<trace-id>-<span-id>-01",
  "timestamp": "2026-10-11T15:30:00Z"
}
```

---

## Observability Instrumentation

**Trace Context (W3C Standard):**
- 100% sampling rate (dev environment) — all requests traced
- Headers: `traceparent`, `tracestate`
- Propagated across all inter-service calls

**Traces (Tempo):**
- Every HTTP request creates spans
- CartService → CatalogService call creates child span
- Complete request flow reconstructible in Grafana

**Metrics (Prometheus):**
- Request latency (p50, p95, p99)
- Request count (total, by endpoint)
- Error rate (by service, by endpoint)

**Logs (Loki):**
- Structured logs with trace ID
- Log levels: DEBUG, INFO, WARN, ERROR
- Errors include stack traces in logs (never in responses)

**Dashboards (4 Required):**
1. User Journey: View → Add → Checkout latency per step
2. Service Health: Uptime, error rates, latency per service
3. Observability Stack: Collector ingestion, backend storage, disk usage
4. Microservices Map: Service dependencies, call volumes

---

## Code Quality Standards

**C# Conventions:**
- **Classes:** PascalCase (CartService, CartController)
- **Methods:** PascalCase (AddItemAsync, ValidateStock)
- **Parameters:** camelCase (cartId, productId)
- **Private fields:** camelCase with underscore (_logger, _repository)

**Async/Await (MANDATORY):**
- All I/O operations (HTTP, database queries) MUST be async
- All async methods end with `Async` suffix
- No `.Result` or `.Wait()` blocking

**Dependency Injection:**
- Constructor injection ONLY (no property injection, no service locator)
- All dependencies declared in constructor
- Services registered in Program.cs with appropriate lifetimes (Scoped for services)

**Error Handling:**
- Use custom exceptions for business logic errors
- Never throw generic Exception
- Never use exceptions for control flow
- Log full exception to logs; return generic message to client

**Code Review:**
- All code reviewed before merge to main
- Checklist: naming, async correctness, no hardcoded secrets, tests included
- Mandatory approval from code owner

---

## API Design & Versioning

**URL Versioning:**
- Format: `/v1/...`, `/v2/...`
- Rationale: Explicit in logs, simple to trace, no router complexity
- Example endpoints:
  - GET `/v1/catalog/products` → List products
  - POST `/v1/cart` → Create cart
  - POST `/v1/orders` → Create order

**Response Format Consistency:**
- All endpoints return ApiResponse<T>
- All errors follow standard error format
- All responses include TraceId for debugging

**HTTP Status Codes:**
- 200 OK, 201 Created, 400 Bad Request
- 404 Not Found, 500 Server Error
- Consistent across all services

---

## Security Baseline

**Input Validation (REQUIRED):**
- All user inputs validated before use
- Model validation + business rule validation
- No exceptions for malformed input

---

## Summary Confirmation

Does this practices summary reflect what your team affirms?

[Answer]: