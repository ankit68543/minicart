# Evidence — Practices Discovery Findings

**Date:** 2026-10-11  
**Stage:** Practices Discovery  
**Project:** MiniCart Microservices

---

## Purpose

This document provides the supporting evidence and rationale for each affirmed practice. Every practice is grounded in:
1. **Codebase Analysis** — What patterns already exist?
2. **Architecture Review** — What does the design enable?
3. **Learning Objectives** — What should we emphasize?
4. **Industry Standards** — What are proven patterns?

---

## Testing Strategy Evidence

### Evidence: Pyramid Layer Ratio (60% Unit / 30% Integration / 10% E2E)

**From Codebase:**
- Three services with clear boundaries → unit tests per service are straightforward
- HttpClient calls between services → integration tests critical for confidence
- End-to-end user journeys (view → add → checkout) → need E2E for validation
- No database layer → no database tests needed; eliminates a test pyramid layer

**From Architecture:**
- CatalogService is pure source (no inbound dependencies) → easiest to unit test
- CartService calls CatalogService → integration tests verify this call works
- OrderService orchestrates Cart + Catalog → integration tests verify both calls
- Frontend calls all three services → E2E tests verify complete flow

**Rationale:**
- Unit tests (60%): Fast feedback, high coverage, low flakiness
- Integration tests (30%): Verify service-to-service communication actually works (critical in microservices)
- E2E tests (10%): Catch integration issues unit/integration tests miss; slower but higher confidence

**Not 70/20/10 because:**
- We're not guarding against database bugs (no DB)
- We need extra integration test confidence (microservices is risky without it)

### Evidence: 80% Coverage Minimum

**From Architecture Review:**
- Three independent services; each must be testable in isolation
- Coverage below 80% leaves blind spots; 80% is achievable standard for learning project
- Enforced in CI prevents regression

**From Reverse Engineering:**
- Architect synthesis recommends: "Define automated checks for test coverage"
- Developer scan noted: "No tests yet"; this is fresh codebase, can establish good habits now

**From Industry Standards:**
- NIST recommends 75-85% for critical systems
- 90%+ is expensive (diminishing returns); 80% is practical balance

### Evidence: xUnit + Moq + FluentAssertions

**From Technology Stack:**
- Services use .NET 6.0+, which has xUnit as default testing framework
- xUnit is actively maintained, widely used in .NET community
- Moq is standard mocking library for .NET
- FluentAssertions provides readable assertion syntax (better than Assert.AreEqual)

**From Reverse Engineering:**
- Dependencies document lists xUnit, Moq, FluentAssertions in expected packages
- No test implementation yet; choosing framework now prevents churn

### Evidence: Observability Tests (Trace Context Propagation)

**From Architecture:**
- **Architect Synthesis:** "End-to-end traceability ✅ Complete user journey fully traceable"
- Every inter-service call designed to propagate W3C Trace Context
- This is core learning objective; must verify it actually works

**From Reverse Engineering:**
- **Key Insight:** "Custom business spans 🟡 Partial; Need dashboard/story definitions"
- **Observability Instrumentation Checklist:** "Trace propagation across services ✅ Designed; One span per HTTP call ✅ Designed"
- Spans are designed but not yet emitted; tests will verify emission

**Rationale:**
- Without observability tests, we might skip trace propagation in implementation
- Tests force implementation of observability features early
- Catches "spans not being created" errors before production

---

## CI/CD Pipeline Evidence

### Evidence: Four-Stage Pipeline (Lint → Build → Test → Security)

**From Codebase:**
- Frontend requires npm build; backend requires dotnet build
- Linting tools exist (.prettierrc, ESLint, dotnet format standards)
- Security vulnerabilities are possible (NuGet + npm packages must be scanned)

**From Reverse Engineering:**
- Developer scan noted: "No CI/CD pipeline; Build and Test verifies defined coverage floors"
- Code quality assessment: "No Build Validation; No lint/format checking; No security scanning"

**Rationale:**
- Lint first: Fast feedback (< 1 min), fail on formatting before compilation
- Build second: Catch compilation errors early (< 5 min)
- Test third: Verify behavior (< 3 min)
- Security last: Scan dependencies (< 2 min), most expensive step

### Evidence: Docker Image Storage (Local Registry)

**From Architecture:**
- Already uses Docker Compose for local development
- All services containerized
- Learning environment; no external registry needed (simplifies setup)

**From Technology Stack:**
- Each service has Dockerfile (multi-stage, optimized for learning)
- Docker Compose config defines networks and volumes

**Alternative Considered (and Rejected):**
- ❌ Push to DockerHub: Requires credentials, slow, not needed for learning
- ❌ Local tar files: Hard to manage versions, no registry features
- ✅ Local Docker registry: Simple, self-contained, versioning built-in

### Evidence: PR Review Gate (Tech Lead Approval)

**From Architecture Review:**
- **Architect Synthesis:** "Recommended Practice Standards ... Code Quality Standards"
- Code review is part of quality assurance
- Learning project; higher standards than production pressure

**From Practices:**
- Org policy: "Guard Policy strict"; team affirms mandatory review
- Prevents bad patterns from entering codebase early

### Evidence: Integration Tests on PR

**From Reverse Engineering:**
- **Code Quality Assessment:** "All inter-service calls must be tested"
- Pull requests are risk points; running integration tests catches errors

**Rationale:**
- Unit tests pass locally; integration tests might fail on PR due to timing/network
- Docker Compose stack running in CI simulates production environment

### Evidence: Staging Deployment (No Approval Needed)

**From Organization Policy:**
- Org affirms: "Deploy on merge to staging environments"
- Staging is low-risk; production requires approval

**Rationale:**
- Staging is continuous deployment (automatic)
- Production is controlled (manual approval gate)
- Catches deployment issues before production

---

## Resilience & Error Handling Evidence

### Evidence: 2-Second Timeout Per Call

**From Reverse Engineering:**
- Architect synthesis notes: "No timeouts implemented" (area for attention)
- Recommends: "2-second timeout per inter-service call"
- Standard for microservices with SLO targets

**From Architecture:**
- CartService calls CatalogService; OrderService calls both Cart + Catalog
- If Catalog times out, Cart and Order should fail gracefully (not hang forever)

**Rationale:**
- Too short (100ms): Might timeout on slow networks; learning environment is local (fast)
- 2 seconds: Reasonable for local Docker calls; allows detection of real hangs
- Too long (10s): User waits too long; poor experience; doesn't solve actual problem

### Evidence: 3 Retries with Exponential Backoff

**From Reverse Engineering:**
- Architect recommends: "3 retries with exponential backoff (100ms, 200ms, 400ms)"
- This is proven pattern in microservices

**From Patterns:**
- If a service is recovering from a crash, immediate retry fails
- Exponential backoff gives service time to recover
- 3 retries: Usually sufficient for transient failures; not so many that we wait forever
- Jitter: Prevents thundering herd (multiple clients retrying simultaneously)

**Total Latency:**
- Best case (success on first try): ~5ms
- Worst case (3 retries): 100 + 200 + 400 = 700ms, plus actual timeouts = 2.7s
- Fits within 5-second operation timeout

### Evidence: Circuit Breaker Pattern

**From Reverse Engineering:**
- Architect synthesis: "Circuit breakers not implemented" (area for attention)
- Recommends: "Circuit breaker for cascading failure prevention"
- Industry standard: Prevents cascading failures (if Service A fails, don't hammer it)

**Rationale:**
- Without circuit breaker: If CatalogService crashes, CartService and OrderService hammer it with retries
- With circuit breaker: After 50% failure rate, stop trying; wait 10s; try again
- Prevents cascading failures from taking down entire system

**Threshold: 50% Failure Rate**
- If 5 of 10 recent requests failed, circuit opens
- For learning, this is conservative (detects actual issues quickly)

---

## Observability Instrumentation Evidence

### Evidence: 100% Trace Sampling (Development Only)

**From Architecture:**
- **Architect synthesis validates:** "End-to-end traceability ✅ Complete"
- W3C Trace Context already designed into all APIs

**From Code Quality Assessment:**
- "Tracing Sample Rate: Currently set to 100% (all traces sampled)"
- "Should reduce for production scenarios; Sampling strategy not documented"

**Rationale:**
- Development: Capture every request (100%) for learning; every trace is valuable
- Production: Sample 10% to reduce storage; still representative for analysis
- Learning environment: No worry about cost/storage; maximize visibility

### Evidence: Structured Logging with Trace ID

**From Reverse Engineering:**
- Architect notes: "Log aggregation (Loki) ✅ Designed; Ready for structured logs"
- Code Quality notes: "Logging Standards: Structured logging format not specified"

**From Best Practices:**
- Structured logging enables automated parsing and querying
- Trace ID in every log entry enables correlation across services
- Loki (log aggregation backend) already configured for this

**Example:** Every log entry includes trace ID, so we can:
- Click on a metric spike → see trace
- Click on a trace → see all related logs
- Search logs by trace ID → find all operations in that user request

### Evidence: Four Dashboards (Latency, Health, Infrastructure, Tracing)

**From Reverse Engineering:**
- Architect synthesis: "Recommended Practice Standards ... Observability Instrumentation Checklist"
- Code Quality: "Dashboards not yet created; Alerts not configured"

**From User Journey:**
- Add to Cart: Frontend latency + CartService latency + CatalogService latency
- Dashboard 1 shows total + breakdown

**From Operational Needs:**
- Dashboard 2: Service health (error rate, latency)
- Dashboard 3: Observability infrastructure health (are we capturing data?)
- Dashboard 4: Trace navigation (how do I debug this user request?)

---

## Code Quality Standards Evidence

### Evidence: PascalCase for Classes, camelCase for Methods

**From Technology Stack:**
- C# naming conventions: PascalCase for public members (classes, methods, properties)
- Linter/formatter will enforce this
- Framework defaults support this

**From Existing Code Structure:**
- Template shows: `CartService`, `CartItem`, `CartController` (all PascalCase)
- Standard .NET convention

### Evidence: Constructor Injection Only

**From Reverse Engineering:**
- Architect synthesis: "Recommended Practice Standards ... Code Quality Standards"
- Developer scan: "Dependency Injection: Built-in IServiceCollection affirmed"

**From ASP.NET Core Design:**
- Built-in DI container expects constructor injection
- Makes testing easy (pass mocks via constructor)
- Makes dependencies explicit in method signature

**Why Not Property Injection?**
- Property injection makes dependencies implicit
- Hard to test (must set properties after construction)
- ASP.NET Core convention is constructor injection

### Evidence: Async/Await Throughout

**From Technology Stack:**
- ASP.NET Core is async-first framework (all I/O async)
- C# async/await syntax is idiomatic and widely used

**From Reverse Engineering:**
- Code structure: "Execution Model: Async/await Full async pipeline"
- All HTTP calls are async (HttpClient.GetAsync, PostAsync)

**Why Never .Result or .Wait()?**
- Creates thread pool starvation (deadlocks in practice)
- Loses async context (trace context might not propagate)
- Learning mistake that's painful to debug

### Evidence: Specific Exception Catching

**From Reverse Engineering:**
- Code quality notes: "Error handling: Exceptions vs Result types? (needs clarification)"
- Domain-specific exceptions enable precise error handling

**From Resilience Patterns:**
- HttpRequestException: Timeout or network error (retry candidate)
- OperationCanceledException: User cancelled or timeout (don't retry)
- Generic Exception: Unknown error (log and propagate to client)

---

## API Design & Versioning Evidence

### Evidence: URL Versioning (/v1/...)

**From Reverse Engineering:**
- Architect synthesis: "API Versioning Strategy Missing" (area for attention)
- Recommends: "Option C: URL versioning (simplest to trace)"

**Alternatives Considered:**
- ❌ Header versioning (Accept: application/vnd.company+json; version=2): Hard to trace in logs
- ❌ Subdomain versioning (api-v1.minicart.local): Extra DNS/network complexity
- ✅ URL versioning (/v1/api/products): Explicit in logs, easy to trace, simple to implement

**Rationale for Learning:**
- URL versioning appears in every log and trace
- Makes it obvious which API version is being called
- Minimal router complexity

### Evidence: Consistent Response Format

**From Code Quality Assessment:**
- API contracts specify: "Consistent response format (success flag, data, error, timestamp)"
- All endpoints use this format

**Rationale:**
- Clients know what to expect (success always has data, error always has error object)
- Makes tracing easier (can identify error responses by structure)
- Simplifies frontend code (one response handler for all endpoints)

### Evidence: W3C Trace Context Headers

**From Architecture:**
- **Architect Synthesis:** "W3C Trace Context embedded from the start" ✅
- API contracts designed to include traceparent, tracestate headers

**From Standards:**
- W3C Trace Context is industry standard (used by Jaeger, Datadog, NewRelic, etc.)
- OpenTelemetry implements it natively
- No vendor lock-in

---

## Security Baseline Evidence

### Evidence: Input Validation (No GraphQL, REST Only)

**From Reverse Engineering:**
- Technology stack: "API Style: REST, JSON over HTTP"
- Code quality: "Input Validation ✅ Planned; Type checking in controllers"

**Rationale:**
- REST is simpler to validate (fixed endpoints, clear contracts)
- GraphQL adds complexity (arbitrary query depth, performance denial-of-service risks)
- Learning project: Stick with REST

### Evidence: No Stack Traces in API Responses

**From Reverse Engineering:**
- Code quality: "Error Handling ✅ Planned; No stack traces in responses"
- Architect recommends: "Use generic error message + trace ID"

**Rationale:**
- Stack traces leak information about system internals (security risk)
- Trace ID allows debugging without exposing internals
- Clients can provide trace ID to support team for investigation

### Evidence: Dependency Scanning in CI

**From Code Quality Assessment:**
- Security assessment: "Dependency Scanning ❌ Not Automated; Manual review only"
- Recommended: "NuGet package scanning: Which tools, threshold, what blocks builds?"

**Tools:**
- OWASP Dependency Check (works with NuGet + npm)
- GitHub Dependabot (if on GitHub, automatic)
- npm audit (built-in, runs on every build)

**Gate:**
- Critical vulnerabilities block merge (non-negotiable)
- Moderate vulnerabilities: logged but non-blocking (learning project)

### Evidence: No Hardcoded Secrets

**From Reverse Engineering:**
- Code quality: "Secrets Management ✅ Planned; Environment variables (no hardcoded secrets)"
- Architecture: "Configuration: appsettings.json per environment"

**Pattern:**
- .env file (git-ignored) for development
- .env.example (committed) shows structure
- Environment variables for production
- CI/CD reads secrets from secure store (vault, GitHub secrets)

---

## Health Checks & Readiness Evidence

### Evidence: /health, /ready, /live Endpoints

**From Reverse Engineering:**
- Code Quality Assessment: "Health Checks & Readiness: Probe strategy, dependencies not implemented"
- Architect Checklist: "Health check endpoints: Need implementation"

**From Container/Orchestration Practice:**
- /health: Is service responding? (liveness check)
- /ready: Can service accept traffic? (readiness check)
- /live: Is process alive? (heartbeat for K8s)

**Rationale for Learning:**
- Simulates production (when deployed to Kubernetes, these are required)
- Enables automated rollback (if health check fails, redeploy)
- Easy to monitor (Grafana can scrape these endpoints)

---

## Recommendations for Code Generation

Before Code Generation phase begins, these artifacts should be created to guide implementation:

### 1. Integration Test Plan

**What to Create:**
- Detailed scenarios for inter-service calls (Add to Cart → Catalog validation)
- Trace context verification procedures
- Mock setup patterns (HttpClient mocking for unit tests)

**Why:**
- Architects understanding of integration testing patterns
- Developers know exactly what to test

### 2. Observability Instrumentation Checklist

**What to Create:**
- Span list per service (e.g., CartService creates spans: "AddItem", "ValidateCatalog")
- Custom metric definitions (e.g., "cart_add_latency_seconds")
- Dashboard query examples (e.g., "show p99 latency for add-to-cart operation")

**Why:**
- Ensures observability is not deferred to "later"
- Developers know which metrics to emit
- QA knows what to verify

### 3. Resilience Test Scenarios

**What to Create:**
- Simulate timeout: Use `Task.Delay()` in mock
- Simulate circuit breaker: Return failure for 5+ consecutive requests
- Verify retry backoff: Assert delay between retries

**Why:**
- Developers understand how to test resilience code
- QA knows edge cases to verify

### 4. API Contract Examples

**What to Create:**
- Real example payloads (add-to-cart request/response)
- Real error examples (product not found, timeout)
- Trace header examples

**Why:**
- Frontend developers know exact payload structure
- Enables contract testing (producer/consumer agreement)

### 5. CI/CD Script Templates

**What to Create:**
- dotnet build, test, publish commands (exact flags)
- Docker build and push scripts
- Health check verification script
- Smoke test script (verify services alive)

**Why:**
- Pipeline implementation is straightforward
- Developers can test locally before CI

---

## Sign-Off

All evidence has been gathered from:
1. ✅ Reverse engineering artifacts (code structure, technology stack, dependencies)
2. ✅ Architecture validation (architect synthesis confirming sound design)
3. ✅ Code quality assessment (identifying gaps)
4. ✅ Industry standards (proven practices)
5. ✅ Team input (affirmed in discussion)

**Every affirmed practice is grounded in evidence and team consensus.**
