**Collaborator:** aidlc-devsecops-agent

## Security & DevSecOps Practices

Affirmed for MiniCart microservices security baseline:

### Input Validation & Data Sanitization

**Rule:** All user input MUST be validated before use. No exceptions.

**Required Validations:**

1. **Request Validation (ASP.NET Model Validation)**
   ```csharp
   public class AddToCartRequest
   {
       [Required(ErrorMessage = "CartId is required")]
       [StringLength(256, MinimumLength = 1)]
       public string CartId { get; set; }
       
       [Required]
       [Range(1, int.MaxValue, ErrorMessage = "ProductId must be > 0")]
       public int ProductId { get; set; }
       
       [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000")]
       public int Quantity { get; set; }
   }
   ```

2. **Manual Validation for Business Rules**
   ```csharp
   if (request.Quantity > MAX_CART_ITEMS)
       throw new ArgumentException($"Quantity exceeds maximum ({MAX_CART_ITEMS})");
   
   if (!Guid.TryParse(request.CartId, out _))
       throw new ArgumentException("CartId must be a valid GUID");
   ```

3. **String Input Sanitization**
   - No SQL injection (using parameterized queries, not applicable for in-memory)
   - No path traversal (validate file paths if file operations added)
   - No command injection (never pass user input to shell)
   - Whitelist approach: Accept only known-good patterns

### Error Handling & Information Disclosure

**Rule:** Never expose internal details in error responses.

✅ **DO:**
```csharp
catch (CartNotFoundException ex)
{
    return NotFound(new ApiResponse
    {
        Success = false,
        Error = "Cart not found",  // Generic message
        TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });
}
```

❌ **DON'T:**
```csharp
catch (Exception ex)
{
    return StatusCode(500, new ApiResponse
    {
        Success = false,
        Error = ex.Message,  // Exposes implementation details
        StackTrace = ex.StackTrace  // Never include stack traces in responses!
    });
}
```

**Logging Strategy:**
- Log full exception (with stack trace) to application logs
- Return generic error message to client
- Include TraceId so errors can be investigated

```csharp
catch (Exception ex)
{
    var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    _logger.LogError(ex, "Unexpected error processing cart. TraceId: {TraceId}", traceId);
    
    return StatusCode(500, new ApiResponse
    {
        Success = false,
        Error = "An error occurred processing your request",
        TraceId = traceId
    });
}
```

### Dependency Scanning & Vulnerability Management

**NuGet Package Scanning:**

Tool: **Snyk** or **OWASP Dependency Check**

```bash
# CLI check
dotnet list package --vulnerable

# In CI pipeline
snyk test --severity-threshold=high
```

**Policy:**
- ❌ **BLOCK:** Critical severity (CVSS 9.0+)
- ❌ **BLOCK:** High severity (CVSS 7.0-8.9) with no available patch
- ⚠️ **WARN:** High severity with patch available (update before release)
- ✅ **ALLOW:** Medium/Low severity with known mitigations

**npm Dependency Scanning (Frontend):**

```bash
npm audit --audit-level=moderate
```

**Update Strategy:**
- Patch versions: Update immediately in CI
- Minor versions: Update weekly in scheduled runs
- Major versions: Review changelog, plan migration before updating

### Secrets Management

**Rule:** Secrets NEVER committed to repository.

**Development Environment:**
- Use `appsettings.Development.json` (gitignored)
- Never put secrets in `appsettings.json`
- Use `dotnet user-secrets` for local development

```bash
dotnet user-secrets set "ApiKeys:CatalogService" "dev-key-12345"
```

**Configuration Binding:**
```csharp
public class ServiceSettings
{
    public string CatalogServiceKey { get; set; }
    public string OrderServiceKey { get; set; }
}

// In Program.cs
var settings = new ServiceSettings();
configuration.GetSection("ServiceSettings").Bind(settings);
```

**Test Environment:**
- Secrets injected via CI/CD pipeline (GitHub Actions secrets)
- Never stored in test code

**Production (Future):**
- Use Azure Key Vault or AWS Secrets Manager
- Rotate keys monthly
- Audit access logs

### Code Review Requirements

**All code MUST be reviewed before merge to main:**

Review Checklist:
- ✅ No hardcoded secrets or credentials
- ✅ Input validation on all user inputs
- ✅ Error responses don't leak information
- ✅ No SQL injection vectors (N/A for now)
- ✅ No XSS vulnerabilities in frontend (separate review)
- ✅ No buffer overflows (not applicable for C#)
- ✅ Dependencies have no critical vulnerabilities
- ✅ Logging doesn't capture sensitive data (passwords, tokens, PII)
- ✅ Exception handling doesn't swallow errors
- ✅ Race conditions considered in concurrent code

**Review Tool:** GitHub PR reviews (mandatory approval from code owner before merge)

### Secure Logging

**Rule:** Never log sensitive data.

✅ **DO:**
```csharp
_logger.LogInformation("User {UserId} added product {ProductId} to cart {CartId}", 
    userId, productId, cartId);
```

❌ **DON'T:**
```csharp
_logger.LogInformation("User authenticated with password {Password}", userPassword);
_logger.LogInformation("Processing payment for card {CardNumber}", cardNumber);
_logger.LogInformation("API request: {FullRequest}", request);  // May contain secrets
```

**Sensitive Data Patterns:**
- Passwords, API keys, tokens
- Personal identifiable information (PII): email, phone, SSN
- Payment information: credit card numbers
- Health/medical information
- Authentication headers (strip before logging)

### Threat Modeling Assessment

**For MiniCart (Learning Project):**

| Threat | Impact | Likelihood | Mitigation |
|--------|--------|-----------|-----------|
| **Injection** | High | Low | Input validation, no dynamic SQL |
| **Authentication** | High | N/A | N/A (no auth in learning project) |
| **CORS** | Medium | Low | Configure CORS policy restrictively |
| **DDoS** | Medium | Low | Rate limiting (optional for learning) |
| **Data Exposure** | Medium | Low | No sensitive data stored; logs sanitized |
| **Broken Access Control** | High | N/A | N/A (no access control) |
| **Insecure Dependencies** | Medium | Medium | Dependency scanning in CI |
| **Logging/Monitoring** | Medium | Medium | Structured logs with trace IDs |

**Future Upgrades (Post-Learning):**
- Add authentication (OAuth2/OpenID Connect)
- Add authorization (role-based access control)
- Add rate limiting
- Add Web Application Firewall (WAF)
- Add mTLS between services

### Health Checks & Readiness

**Endpoints (for orchestration & monitoring):**

```csharp
// /health — Is service running?
[HttpGet("health")]
public IActionResult Health()
{
    return Ok(new { status = "healthy" });
}

// /ready — Is service ready to accept requests?
[HttpGet("ready")]
public async Task<IActionResult> Ready()
{
    // Check dependencies: database, other services
    var catalogServiceReady = await CheckCatalogServiceAsync();
    
    if (!catalogServiceReady)
        return StatusCode(503, new { status = "not ready" });
    
    return Ok(new { status = "ready" });
}

// /live — Is service process alive?
[HttpGet("live")]
public IActionResult Live()
{
    return Ok(new { status = "alive" });
}
```

**Usage in Docker Compose:**
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:5001/health"]
  interval: 10s
  timeout: 3s
  retries: 3
```

### Dependency Vulnerability Baseline

**Current Status:** No known vulnerabilities in template.

**Scanning cadence:** Every PR + Weekly scheduled scan

**Artifact:** Dependency vulnerability report in CI logs

### OWASP Top 10 Compliance (Learning Baseline)

| # | Category | Status |
|---|----------|--------|
| 1 | Injection | ✅ Mitigated (input validation) |
| 2 | Broken Authentication | 🔄 N/A (no auth in learning) |
| 3 | Sensitive Data Exposure | ✅ Mitigated (no sensitive data; logs sanitized) |
| 4 | XML External Entities | ✅ N/A (no XML parsing) |
| 5 | Broken Access Control | 🔄 N/A (no access control in learning) |
| 6 | Security Misconfiguration | ✅ Mitigated (secure defaults) |
| 7 | Cross-Site Scripting | 🔄 Frontend review needed (separate) |
| 8 | Insecure Deserialization | ✅ Mitigated (input validation) |
| 9 | Using Components with Known Vulnerabilities | ✅ Mitigated (dependency scanning) |
| 10 | Insufficient Logging & Monitoring | ✅ Mitigated (structured logging) |

### Sign-Off

Security and DevSecOps baseline affirmed for learning project. Practices ready for Code Generation.

**DevSecOps consent:** Affirmed ✅
