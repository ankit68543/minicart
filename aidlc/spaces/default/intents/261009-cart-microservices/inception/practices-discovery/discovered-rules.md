# Discovered Rules — MiniCart Practices

**Date:** 2026-10-11  
**Stage:** Practices Discovery  
**Project:** MiniCart Microservices

---

## Rule Classification Framework

Rules are classified by scope based on reusability and durability:

- **Org-Level Rules** — Apply to all projects; foundational practices
- **Team-Level Rules** — This team's affirmed practices; typically durable across projects
- **Project-Level Rules** — Specific to MiniCart; project constraints or specializations

---

## Org-Level Rules (Already Affirmed, Reconfirmed)

These rules are already in `aidlc/spaces/default/memory/org.md` and remain applicable to MiniCart:

### Way of Working
✅ **AFFIRM:** Trunk-based development with short-lived feature branches (1-2 days)  
✅ **AFFIRM:** Squash-merge Bolt branches into main (one commit per Bolt)  
✅ **AFFIRM:** One trunk for all environments; gate production via tags/configs, not branches

### Testing Posture
✅ **AFFIRM:** Tests are first-class deliverables  
✅ **AFFIRM:** Default methodology: test-after; ordered: implement, then test  
✅ **AFFIRM:** Scopes `mvp`, `enterprise`, `feature`, `infra` require 80% line coverage + CI execution before merge  
✅ **AFFIRM:** Existing test suite must remain green for all scopes

### Deployment
✅ **AFFIRM:** Deploy on merge to staging environments  
✅ **AFFIRM:** Production deploys gate on manual approval (tech lead + product owner)

### Code Style
✅ **AFFIRM:** Defer to project linter config; formatter runs in CI; linter failure blocks PR  
✅ **AFFIRM:** Naming conventions: language-idiomatic (C# = PascalCase for classes, camelCase for methods)

---

## Team-Level Rules to Affirm in team.md

These are new team-level practices affirmed during this Practices Discovery stage.

### Way of Working

```
## Way of Working

**Affirmed 2026-10-11**

We use trunk-based development (org-affirmed) with these team-specific practices:

- Feature branches live max 2 days; long-lived branches accumulate merge debt
- All work merges to `main` via short-lived feature branches
- Pull requests require code review (tech lead or senior developer) before merge
- Squash-merge policy (one commit per Bolt) keeps main clean and traceable
- Hotfixes: Branch from `main`, fix, merge back with expedited review (same day)

We affirm walking skeleton OFF for this project. The deployment pipeline is simple enough
that the upfront slice cost outweighs the learning value. Run full Construction normally.
```

### Testing Posture

```
## Testing Posture

**Affirmed 2026-10-11**

We use test-after methodology with three-layer pyramid:

- **Methodology:** Test-after (implement each layer, then write tests)
- **Ordering:** Implement service layer → write unit tests → integrate with other services
- **Pyramid:** Unit (60%) + Integration (30%) + E2E (10%)
- **Coverage Floor:** 80% line coverage minimum per service, enforced in CI
- **Integration Tests:** All inter-service HTTP calls must have integration tests
- **Contract Tests:** Producer verifies response format before consumer implements
- **E2E Tests:** User journeys (view → add → checkout) tested end-to-end with Playwright
- **Observability Tests:** All inter-service calls must propagate trace context (verified in tests)

Testing tools affirmed:
- **Framework:** xUnit (C#), Playwright (frontend)
- **Mocking:** Moq (services), HttpClient mocks (HTTP calls)
- **Assertions:** FluentAssertions (readable test output)
- **Environment:** Docker Compose (local integration stack)

Existing test suite (bootstrap) remains green. New tests added incrementally per Bolt.
```

### CI/CD Practices

```
## CI/CD Practices

**Affirmed 2026-10-11**

Our CI/CD pipeline runs automatically on every push:

**On Feature Branch Push:**
1. Lint & format check (dotnet format, ESLint)
2. Build (dotnet build, npm run build)
3. Unit tests (≥80% coverage)
4. Security scan (OWASP Dependency Check)

**On Pull Request:**
5. Code review gate (tech lead approval required)
6. Integration tests (service-to-service communication)
7. Trace context verification (manual or automated)

**On Merge to Main:**
8. Full test re-run (unit + integration)
9. Docker image build & push (per service)
10. Deployment to staging (manual approval)

**Artifacts:**
- Docker images tagged with commit SHA
- Stored in local registry (learning environment)
- Retention: Keep last 10 builds per service

**Gates:**
- Linter failure → Developer fixes locally
- Build failure → Cannot proceed
- Unit test failure or coverage < 80% → Cannot proceed
- Critical security vulnerability → Cannot proceed
- Code review required → Tech lead must approve

**Deployment:**
- Staging deploys automatically after merge (no approval needed)
- Production deploys manual (tech lead + product owner sign-off)
- Smoke tests verify service health post-deployment
```

### Resilience & Error Handling

```
## Resilience & Error Handling

**Affirmed 2026-10-11**

We treat resilience as mandatory, not optional:

**Timeouts:**
- Inter-service call timeout: 2 seconds per call
- Total operation timeout: 5 seconds (allows 2-3 retries)
- Configurable per environment in appsettings.json

**Retries:**
- Max retries: 3
- Exponential backoff: 100ms → 200ms → 400ms
- Jitter: ±10% randomization to avoid thundering herd
- Idempotent-only: Retry GET only; POST/PUT tracked by request ID

**Circuit Breaker:**
- Threshold: 50% failure rate over last 10 requests
- Open duration: 10 seconds
- Reset on half-open success

**Error Responses:**
- No stack traces to clients; trace ID provided for debugging
- Standard error response format: {success, data, error, timestamp}
- Error codes defined per service (INVALID_REQUEST, NOT_FOUND, TIMEOUT, INTERNAL_ERROR)

**Fallback:**
- No stale-data fallbacks in learning environment
- Fail fast and clearly with 503 Service Unavailable
```

### Observability Instrumentation

```
## Observability Instrumentation

**Affirmed 2026-10-11**

Observability is built in from day one, not bolted on later:

**Trace Strategy:**
- Sampling: 100% in development (capture every request for learning)
- Standard: W3C Trace Context (traceparent, tracestate headers)
- Propagation: Every inter-service call MUST include traceparent header
- Framework: OpenTelemetry .NET SDK auto-instruments ASP.NET Core + HttpClient

**Metrics:**
- Per-endpoint latency (p50, p95, p99)
- Request count by method/status
- Outbound call latency per service
- Memory and CPU utilization
- Exemplars linked to sample traces

**Logs:**
- Structured logging with trace ID in every entry
- Levels: DEBUG, INFO, WARN, ERROR, FATAL
- PII redaction: No passwords, payment info, or SSNs
- All observability backends: Prometheus (metrics), Loki (logs), Tempo (traces)

**Dashboards (Grafana):**
- User Journey Latency (add-to-cart end-to-end timing)
- Service Health (error rate, latency by service)
- Observability Infrastructure (collector health, backend storage)
- Trace Navigation (Tempo search and span tree)

**Alerts:**
- Service Unavailable (any service 503 for > 1 min)
- High Error Rate (> 5% for > 5 min)
- High Latency (p99 > 1 second for > 5 min)
- Dependency Timeout (circuit breaker open > 10 sec)
```

### Code Quality Standards

```
## Code Quality Standards

**Affirmed 2026-10-11**

Code is a team artifact; we write for clarity and testability:

**Naming Conventions:**
- Classes & structs: PascalCase (CartService, CartItem)
- Methods & properties: PascalCase (AddItem(), GetTotal())
- Local variables: camelCase (cartId, itemCount)
- Private fields: camelCase with _ prefix (_logger, _httpClient)
- Constants: UPPER_SNAKE_CASE (MAX_CART_SIZE, DEFAULT_TIMEOUT)

**Async/Await:**
- Always use async/await; never block threads with .Result or .Wait()
- All I/O methods are async (GetAsync(), PostAsync(), etc.)
- Async methods return Task or Task<T>

**Dependency Injection:**
- Constructor injection always; no service locators
- All dependencies declared in constructor
- Enable testing via mock injection

**Error Handling:**
- Catch specific exceptions (HttpRequestException, OperationCanceledException)
- Log full exception with context
- Throw domain-specific exceptions (DependencyException, TimeoutException)
- Never swallow exceptions silently

**Comments & Documentation:**
- Public methods documented with XML comments (///)
- Complex logic explained; obvious code not commented
- Example: exponential backoff math, retry strategy reasoning

**File Organization:**
- Controllers: HTTP binding only
- Services: Business logic
- Models: Data structures
- Middleware: Cross-cutting concerns
- Config: Settings and initialization

**Code Review Checklist:**
- Code follows naming conventions
- Methods are testable (injectable dependencies)
- Error handling is specific and logged
- No hardcoded secrets
- No exceptions leaked to clients
- Trace context preserved in async operations
```

### API Design & Versioning

```
## API Design & Versioning

**Affirmed 2026-10-11**

APIs are contracts. Once published, they must remain stable or offer clear migration:

**Versioning:**
- Strategy: URL path versioning (/v1/api/..., /v2/api/...)
- Reason: Simple to trace, explicit in logs and dashboards
- Current: v1 for all services

**Response Format (Consistent):**
- Success: {success: true, data: {...}, error: null, timestamp}
- Error: {success: false, data: null, error: {code, message, traceId}, timestamp}

**HTTP Status Codes:**
- 200: OK (successful GET/PUT)
- 201: Created (successful POST)
- 400: Bad Request (malformed request)
- 404: Not Found (resource missing)
- 500: Internal Error (server error)
- 503: Service Unavailable (dependency timeout)

**Documentation:**
- All endpoints documented with curl example, schema, status codes, trace context requirement

**Deprecation:**
- Release v2 alongside v1 (both active)
- Announce 6-month sunset date
- Include Deprecation header in v1 responses
- Provide migration guide
- Return 410 Gone after sunset

**W3C Trace Context:**
- Every endpoint requires traceparent header support
- Propagated in inter-service calls automatically via OpenTelemetry
```

### Security Baseline

```
## Security Baseline

**Affirmed 2026-10-11**

Simple security practices for learning environment:

**Input Validation:**
- Validate all controller input (positive integers, non-null fields)
- Return 400 Bad Request for invalid input with clear message

**Error Messages:**
- Never leak stack traces to clients
- Include trace ID for internal debugging
- Use 500 Internal Error with generic message

**Dependency Scanning:**
- NuGet audit on every PR build (OWASP Dependency Check)
- npm audit on every PR build (npm audit)
- Critical vulnerabilities block merge
- Moderate-level warnings logged but non-blocking

**Secrets Management:**
- .env file (git-ignored) for development
- .env.example (committed) shows structure
- Environment variables for production
- Never commit real credentials

**Code Review:**
- Mandatory tech lead approval before merge
- Review checklist: no hardcoded secrets, input validation, error message safety
```

### Health Checks

```
## Health Checks & Readiness

**Affirmed 2026-10-11**

Services report their health for monitoring and orchestration:

**Endpoints:**
- GET /health — Service and dependency status
- GET /ready — Ready to accept traffic
- GET /live — Process alive (for K8s liveness)

**Response Format:**
- {status: "healthy|degraded|unhealthy", service, timestamp, dependencies}
- Healthy: 200 OK
- Degraded: 200 OK (receiving traffic, but reduced capacity)
- Unhealthy: 503 Service Unavailable

**Dependency Checks:**
- CatalogService: Check if responding to health check
- OrderService: Check if responding to health check
- OTEL Collector: Check if accepting telemetry (connection test)
```

---

## Project-Level Rules to Add to project.md

These are MiniCart-specific constraints and specializations.

### Tech Stack

```
## Tech Stack

**Locked for MiniCart (Affirmed 2026-10-11)**

**Backend Services:**
- ASP.NET Core 6.0+ (C# 11+)
- Target framework: net6.0 (or later .NET LTS)
- Razor Pages: Not used; REST API only

**Frontend:**
- Next.js 14+ (App Router, not Pages Router)
- React 18+
- TypeScript 5.0+
- No UI framework lock-in; CSS utilities only (Tailwind optional)

**Observability:**
- OpenTelemetry SDK (vendor-neutral)
- Prometheus (metrics)
- Loki (logs)
- Tempo (distributed traces)
- Grafana (visualization)

**Container & Deployment:**
- Docker (container images)
- Docker Compose 3.9+ (local orchestration)
- No Kubernetes in learning phase (add if production-bound)

**Build & Package Management:**
- MSBuild / dotnet CLI (C#)
- npm 10+ (Node.js)
- NuGet (package manager)

**Testing:**
- xUnit (C# testing framework)
- Moq (mocking)
- Playwright (browser automation)
- FluentAssertions (assertions)

NEVER: Change runtime without team discussion (core learning objective)
NEVER: Add database; stay in-memory (learning scope)
NEVER: Use GraphQL; stick to REST (simpler for tracing)
```

### Walking Skeleton

```
## Walking Skeleton

**Project Specialization (Affirmed 2026-10-11)**

We affirm walking skeleton OFF for MiniCart.

Rationale:
- Services are simple (no complex domain modeling)
- Deployment pipeline is straightforward (Docker Compose)
- Observability infrastructure is pre-configured
- Early slice cost outweighs learning value at this maturity

Instead: Run full Construction normally, verify observability end-to-end during Code Generation.
```

### Decided

```
## Decided

**Decisions from Practices Discovery (Affirmed 2026-10-11)**

DECIDED: Use synchronous REST/HTTP for inter-service communication (not async messaging).
  Rationale: Learning-appropriate; simpler tracing; no message broker complexity.
  (Practices Discovery, 2026-10-11)

DECIDED: In-memory data storage for all services (no database).
  Rationale: Learning scope; no persistence learning goal; simplifies setup.
  (Reverse Engineering validated, 2026-10-11)

DECIDED: No authentication/authorization in this version.
  Rationale: Learning environment; open APIs focus on architecture and observability.
  (Practices Discovery, 2026-10-11)

DECIDED: 100% trace sampling in development; 10% in production (future).
  Rationale: Every request captured for learning; sample in production to reduce noise.
  (Practices Discovery, 2026-10-11)

DECIDED: W3C Trace Context as trace propagation standard.
  Rationale: Industry standard; supported natively by OpenTelemetry.
  (Practices Discovery, 2026-10-11)

DECIDED: URL versioning for APIs (/v1/...).
  Rationale: Simple to trace; explicit in logs and dashboards; minimal router complexity.
  (Practices Discovery, 2026-10-11)
```

### Scope Overrides

```
## Scope Overrides

**MiniCart-Specific Scope Rules (Affirmed 2026-10-11)**

For Construction bolts in this project:

- **Testing:** All features require integration tests (inter-service communication tests).
  Not just unit tests; we're learning microservices, so service boundaries are critical.

- **Observability:** All features must include observability (spans, metrics, logs).
  Not optional; this is core learning objective. Dashboard evidence required.

- **Code Review:** Tech lead review required for all PRs (not optional fast-track).
  Learning project; quality standards are higher than production pressure might allow.

- **Resilience:** All inter-service calls must include timeout handling.
  Circuit breaker and retry logic required for production-readiness learning.

- **Documentation:** All service endpoints must be documented (curl example, schema).
  Reference implementation; others will learn from this.
```

---

## Mandated Practices

```
## Mandated

**Affirmed Practices (Effective Immediately)**

ALWAYS use async/await for I/O operations. NEVER block threads with .Result or .Wait().

ALWAYS inject dependencies via constructor. NEVER use service locators or static fields.

ALWAYS include traceparent header in inter-service calls. NEVER omit trace context propagation.

ALWAYS validate input in controllers. NEVER trust client-provided data.

ALWAYS include trace ID in error responses. NEVER leak stack traces to clients.

ALWAYS run tests locally before pushing. NEVER rely solely on CI to catch errors.

ALWAYS write tests when code is written. NEVER defer testing to "later".

ALWAYS use W3C Trace Context header format. NEVER use custom trace headers.

ALWAYS log with trace ID. NEVER log events without correlation to trace.

ALWAYS document public methods. NEVER leave API contracts ambiguous.
```

---

## Forbidden Practices

```
## Forbidden

**Practices We Will NOT Use (Effective Immediately)**

NEVER block threads with .Result or .Wait(). Use async/await throughout.

NEVER use service locators or singletons for dependency injection. Use constructor injection.

NEVER catch generic Exception; always catch specific exceptions (HttpRequestException, etc.).

NEVER commit hardcoded secrets to git. Use .env files (git-ignored) or environment variables.

NEVER skip code review. Tech lead approval is mandatory for all merges.

NEVER leak exception stack traces to API clients. Use generic error message + trace ID.

NEVER use GraphQL for inter-service communication. Use REST/HTTP only.

NEVER add a database layer without team discussion. Stay in-memory (learning scope).

NEVER bypass security scanning in CI. Vulnerability scan must pass before merge.

NEVER deploy to production without staging verification. Two-step process: staging → manual approval → production.

NEVER omit trace context in inter-service calls. Every HTTP call propagates traceparent header.

NEVER bypass health checks. Every service must expose /health, /ready, /live endpoints.

NEVER mix authentication mechanisms (JWT + API key, etc.). Defer auth to future phase.

NEVER hardcode service URLs in code. Use configuration (appsettings.json, environment variables).
```

---

## Summary: What Belongs Where?

| Category | Org-Level | Team-Level | Project-Level |
|----------|-----------|-----------|---|
| **Trunk-based dev** | ✅ Yes (org.md) | — | — |
| **Test-after methodology** | ✅ Yes (org.md) | ✅ Affirm pyramid & tools (team.md) | — |
| **Async/await always** | — | ✅ Affirm (team.md) | — |
| **DI requirement** | — | ✅ Affirm constructor-only (team.md) | — |
| **Resilience patterns** | — | ✅ Affirm timeouts/retries/CB (team.md) | — |
| **Observability-first** | — | ✅ Affirm sampling/metrics/logs (team.md) | — |
| **W3C Trace Context** | — | ✅ Affirm as standard (team.md) | — |
| **URL versioning** | — | ✅ Affirm for APIs (team.md) | — |
| **.NET 6.0+ only** | — | — | ✅ Lock for MiniCart (project.md) |
| **No database** | — | — | ✅ Learning scope constraint (project.md) |
| **No auth this phase** | — | — | ✅ MiniCart decision (project.md) |
| **Walking skeleton OFF** | — | — | ✅ MiniCart-specific (project.md) |
| **No GraphQL** | — | — | ✅ MiniCart REST-only (project.md) |

**Key Insight:** Organization-level rules are foundation. Team-level rules are durable practices we'll use on future projects. Project-level rules are MiniCart-specific constraints (tech stack lock, no database, no auth).
