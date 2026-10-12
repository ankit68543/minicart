<!-- INVARIANT: examples are single-line HTML comments so a fresh template parses to total=0 (MEMORY_EMPTY). Do NOT un-comment or split across lines. t100 guards this. -->
> This file is kept up to date automatically while the stage runs. Add observations at the review step, not by editing here directly.

## Interpretations
2026-10-11T14:25:00Z — Test-after methodology affirmed (not TDD); team prioritizes clarity over test-first for learning phase; observability tests will verify implementation
2026-10-11T14:25:00Z — 80% coverage minimum chosen; not 90% to balance effort/learning; observability and resilience are higher priority than blanket coverage
2026-10-11T14:20:00Z — Chose URL versioning for APIs (/v1/...); simpler than header versioning for tracing; explicit in logs and dashboards
2026-10-11T14:20:00Z — Sync/await patterns emphasized; learning project will not explore async messaging (too complex for observability learning focus)

## Deviations
2026-10-11T14:22:00Z — Affirmed walking skeleton OFF (org default is contextual); learning project complexity is low enough that early slice provides no advantage
2026-10-11T14:22:00Z — No health check implementation mandated yet (will be Code Generation task); practices affirmed, implementation deferred

## Tradeoffs
2026-10-11T14:24:00Z — 100% trace sampling in dev vs. 10% in production; chose full sampling to maximize visibility for learning; accept performance cost in dev environment
2026-10-11T14:24:00Z — Manual review + tech lead approval for CI gate vs. auto-approval on green build; chose manual (learning project, higher standards than production)
2026-10-11T14:23:00Z — 2-second timeouts (local Docker network fast) vs. 5-second (safer margin); chose 2s to catch real issues sooner; can adjust after measurement
2026-10-11T14:23:00Z — No circuit breaker per-service vs. global circuit breaker; chose per-service (microservices isolation); adds complexity but better isolation

## Open questions
2026-10-11T14:26:00Z — Dashboard alerts thresholds (error rate > 5%, latency p99 > 1s): Confirm thresholds after first full build; may need tuning
2026-10-11T14:26:00Z — Circuit breaker 50% threshold: Verify this sensitivity with actual failure patterns during Construction
2026-10-11T14:26:00Z — Integration test environment: Confirm Docker Compose performance is acceptable for local development (no resource bottlenecks)
2026-10-11T14:26:00Z — Trace context propagation across frontend: Decide if browser instrumentation is in scope or deferred to Operations phase
