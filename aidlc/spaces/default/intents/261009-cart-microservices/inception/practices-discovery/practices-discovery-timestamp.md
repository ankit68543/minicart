---
timestamp: 2026-10-11T14:30:00Z
stage: practices-discovery
agent: aidlc-pipeline-deploy-agent (hub-and-spoke facilitator)
status: artifacts-complete
---

# Practices Discovery — Scan Metadata

**Project:** MiniCart Microservices  
**Record:** `aidlc/spaces/default/intents/261009-cart-microservices/`  
**Stage:** Inception → Practices Discovery  

---

## Scan Details

| Field | Value |
|-------|-------|
| **Start Time** | 2026-10-11T14:00:00Z |
| **Completion Time** | 2026-10-11T14:30:00Z |
| **Duration** | 30 minutes |
| **Facilitator Agent** | aidlc-pipeline-deploy-agent (hub-and-spoke coordinator) |
| **Evidence Sources** | 9 codekb artifacts + architecture validation |
| **Artifacts Reviewed** | code-structure, technology-stack, dependencies, code-quality-assessment, architecture, business-overview, architect-synthesis, developer-scan, reverse-engineering-timestamp |

---

## Specialist Inputs Synthesized

### Quality Engineer Input
**On Testing Strategy:**
- ✅ Affirm three-layer pyramid: Unit (60%) + Integration (30%) + E2E (10%)
- ✅ 80% coverage minimum per service
- ✅ Integration tests mandatory for inter-service calls
- ✅ Contract tests verify producer/consumer agreement
- ✅ Observability tests verify trace context propagation

**On Test Tools:**
- ✅ xUnit for framework (already in dependencies)
- ✅ Moq for mocking HTTP clients
- ✅ FluentAssertions for readable assertions
- ✅ Playwright for browser automation
- ✅ Docker Compose for local integration environment

### Developer Input
**On Code Structure:**
- ✅ PascalCase for classes, camelCase for methods (C# conventions)
- ✅ Constructor injection always (no service locators)
- ✅ Async/await throughout (never .Result or .Wait)
- ✅ Specific exception catching (not generic)
- ✅ Separation of concerns: Controllers → HTTP binding, Services → logic, Models → data

**On Error Handling:**
- ✅ Custom exception types (DependencyException, TimeoutException)
- ✅ Log full exception context
- ✅ Return error code + message + trace ID to client (no stack traces)

### DevSecOps Input
**On Dependency Scanning:**
- ✅ OWASP Dependency Check for NuGet + npm
- ✅ Run on every PR build
- ✅ Critical vulnerabilities block merge
- ✅ Moderate level warnings logged (non-blocking in learning phase)

**On Secure Coding:**
- ✅ Input validation in all controllers
- ✅ No hardcoded secrets (use .env + environment variables)
- ✅ Mandatory code review (tech lead approval)
- ✅ Error messages sanitized (no internal details)

**On Threat Assessment:**
- ✅ Learning environment simplification: No auth/encryption needed
- ✅ When moving to production: Add JWT + mTLS + transport encryption
- ✅ Data validation prevents SQL injection (N/A for in-memory)
- ✅ OWASP Top 10 considerations documented for future

---

## Decisions Made

| Decision | Rationale | Contingency |
|----------|-----------|------------|
| **Test-After Methodology** | Appropriate for learning; code patterns known | Switch to TDD if quality issues arise |
| **80% Coverage Minimum** | Practical balance; enforced in CI | Increase to 90% after code generation if gaps found |
| **3 Retries, 2-Second Timeouts** | Proven microservice pattern; learning-appropriate | Tune per actual latency measurements |
| **100% Trace Sampling** | Maximum visibility for learning | Reduce to 10% if performance issues |
| **URL Versioning** | Simplest for tracing; explicit in logs | Can migrate to header versioning if needed |
| **Constructor DI Only** | ASP.NET Core best practice; testable | No alternatives considered (mandated) |
| **Async/Await Enforcement** | Prevents deadlocks; idiomatic C# | No alternatives (mandated) |
| **Walking Skeleton OFF** | Deployment pipeline simple enough | Re-evaluate if implementation reveals complexity |
| **Docker Local Registry** | Learning environment simplification | Upgrade to DockerHub/ECR if sharing needed |

---

## Areas for Code Generation Focus

1. ✅ **Observability Instrumentation** — Not yet coded; must emit spans, metrics, logs
2. ✅ **Resilience Patterns** — Timeouts, retries, circuit breakers must be implemented
3. ✅ **Integration Tests** — All inter-service calls must have tests
4. ✅ **Health Check Endpoints** — /health, /ready, /live must be implemented
5. ✅ **Error Handling** — Catch specific exceptions, return consistent error format
6. ✅ **Trace Context Propagation** — Every HTTP call must include traceparent header

---

## Validation Checklist

### Practices Affirmed ✅
- [x] Testing strategy (pyramid, coverage, tools)
- [x] CI/CD pipeline (stages, gates, artifacts)
- [x] Resilience & error handling (timeouts, retries, circuit breakers)
- [x] Observability instrumentation (traces, metrics, logs, dashboards, alerts)
- [x] Code quality standards (naming, async, DI, error handling)
- [x] API design & versioning (URL versioning, response format, status codes)
- [x] Security baseline (input validation, no secrets, dependency scanning)
- [x] Health checks & readiness (endpoints, response format)

### Rules Prepared ✅
- [x] Org-level rules (reconfirmed from memory/org.md)
- [x] Team-level rules (new practices for team.md)
- [x] Project-level rules (MiniCart-specific for project.md)

### Evidence Documented ✅
- [x] Supporting rationale for each practice
- [x] Alternatives considered and rejected
- [x] Industry standards cited
- [x] Architecture validation cross-reference
- [x] Code quality assessment gaps addressed

### Artifacts Complete ✅
- [x] team-practices.md (1,200+ lines, comprehensive)
- [x] discovered-rules.md (layered org/team/project rules)
- [x] evidence.md (supporting analysis and rationale)
- [x] practices-discovery-timestamp.md (this file)

---

## Next Steps: Code Generation Entry Criteria

Before Code Generation can begin, confirm:

1. **Team Consensus:** Team reviews team-practices.md and affirms practices
2. **Memory Updates:** Org-level, team-level, and project-level rules added to memory files
3. **Pre-Generation Artifacts:** Create Integration Test Plan, Observability Checklist, Resilience Test Scenarios, API Contract Examples, CI/CD Script Templates
4. **Developer Onboarding:** Share practices with code generation team; Q&A session if needed

---

## Handoff to Code Generation

**What Code Generation Will Do:**
- Implement services following affirmed practices
- Emit spans, metrics, and logs per observability checklist
- Write integration tests with trace context verification
- Implement resilience patterns (timeouts, retries, circuit breaker)
- Create health check endpoints
- Write CI/CD scripts (dotnet build/test/publish, Docker build/push, smoke tests)

**What This Stage Completed:**
- ✅ Analyzed existing codebase (reverse engineering foundation)
- ✅ Affirmed team practices (hub-and-spoke specialist input synthesis)
- ✅ Documented rules by layer (org/team/project)
- ✅ Provided supporting evidence (alternatives considered, rationale clear)
- ✅ Ready for implementation with clear standards

---

## Timestamp & Audit Trail

- **Practices Discovery Stage:** Inception Phase
- **Start:** 2026-10-11 14:00 UTC
- **Complete:** 2026-10-11 14:30 UTC
- **Facilitator:** Pipeline & Deploy Agent (hub-and-spoke lead)
- **Specialist Inputs:** Quality Engineer, Developer, DevSecOps (synthesized)
- **Artifacts Produced:** 4 files (team-practices.md, discovered-rules.md, evidence.md, this file)
- **Status:** Ready for affirmation gate and Code Generation entry
