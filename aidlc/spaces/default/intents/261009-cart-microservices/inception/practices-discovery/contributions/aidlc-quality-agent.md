**Collaborator:** aidlc-quality-agent

## Quality Engineering Practices

Affirmed for MiniCart microservices testing strategy:

### Testing Pyramid & Strategy

1. **Unit Tests (60% of test count)**
   - Controllers: Happy path + error cases, mocked dependencies
   - Services: Business logic isolation, mocked external calls
   - Models: Serialization/deserialization validation
   - Target: 80% code coverage minimum

2. **Integration Tests (30% of test count)**
   - Inter-service communication: CartService → CatalogService
   - HTTP header propagation: Trace context (traceparent, tracestate)
   - Error scenarios: Timeouts, service unavailable, invalid responses
   - Database interactions: Not applicable (in-memory storage)
   - Testing tools: xUnit for C#, Testcontainers for service dependencies

3. **End-to-End Tests (10% of test count)**
   - Complete user journeys: View Products → Add to Cart → Checkout
   - Full trace context propagation verification
   - Response time measurement and assertion
   - Tools: Selenium or Playwright for frontend, HttpClient for API calls

### Coverage Targets

- **Minimum:** 80% line coverage per service
- **Services with no coverage:** Build failure
- **Dead code:** Code review catch; remove if not used
- **Test-to-code ratio:** Aim for 1:1 (1 line test per 1 line production code)

### Coverage Reporting & CI Integration

- SonarQube integration in CI pipeline
- Coverage report as build artifact
- Coverage regression: Fail build if coverage drops > 2%
- Coverage trend: Track on dashboard (optional)

### Observability Testing

Critical: **Every integration test must verify trace context**

```csharp
[Fact]
public async Task AddToCart_PropagatesTraceContext()
{
    // Arrange
    var traceId = ActivityTraceId.CreateRandom();
    var spanContext = new ActivitySpanId().CreateRandom();
    
    // Act
    var response = await _client.PostAsync(
        "/api/cart",
        content,
        new Dictionary<string, string>
        {
            ["traceparent"] = $"00-{traceId}-{spanContext}-01"
        });
    
    // Assert
    Assert.True(response.Headers.Contains("traceparent"));
    var responseTraceId = ExtractTraceId(response.Headers["traceparent"]);
    Assert.Equal(traceId.ToString(), responseTraceId);
}
```

### Resilience Testing

All integration tests must include resilience scenarios:
- Timeout behavior: Verify 2-second timeout is enforced
- Retry logic: Verify 3 retries with backoff execute correctly
- Circuit breaker: Verify open/half-open/closed states
- Fallback: Verify error response (no silent failures)

### Recommended Test Framework & Tools

| Layer | Tool | Rationale |
|-------|------|-----------|
| **Unit** | xUnit + Moq | ASP.NET standard, clear syntax |
| **Integration** | xUnit + HttpClientFactory | No-dependency testing of HTTP stack |
| **Observability** | DiagnosticListener + Moq | Capture actual Activity/Span events |
| **E2E** | Playwright | Works with Next.js frontend, trace validation |
| **Coverage** | OpenCover + ReportGenerator | ASP.NET standard, HTML reports |
| **Perf** | BenchmarkDotNet | Latency tracking, regression detection |

### Test Automation in CI

- Tests run on every push to feature branch
- Tests run before merge to main
- Coverage report attached to PR
- Failed tests block merge
- Timeout: 15 minutes for full suite

### Sign-Off

Quality baseline affirmed. Testing practices ready for Code Generation. Test cases can be written during Code Generation; automated checks run during Build and Test stage.

**Quality Engineer consent:** Affirmed ✅
