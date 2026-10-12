# Dependencies — MiniCart Microservices

**Scan Date:** 2026-10-09  
**Scope:** Direct and transitive dependencies for all services  

---

## Dependency Overview

```
Web Frontend (Next.js)
  ├─ React (UI)
  ├─ Next.js (Framework)
  ├─ TypeScript (Language)
  ├─ @opentelemetry/* (Telemetry SDKs)
  └─ npm packages (~50-100 total with transitive)

CatalogService (.NET)
  ├─ ASP.NET Core 6.0+ (Framework)
  ├─ OpenTelemetry NuGet packages
  ├─ System.* libraries (base class library)
  └─ NuGet packages (~10-20 total)

CartService (.NET)
  ├─ ASP.NET Core 6.0+
  ├─ OpenTelemetry
  ├─ System.Net.Http (for CatalogService calls)
  └─ NuGet packages (~10-20 total)

OrderService (.NET)
  ├─ ASP.NET Core 6.0+
  ├─ OpenTelemetry
  ├─ System.Net.Http (for inter-service calls)
  └─ NuGet packages (~10-20 total)
```

---

## .NET Service Dependencies

### Framework Dependencies (Implicit with .NET 6.0+)

**FrameworkReference:**
```xml
<FrameworkReference Include="Microsoft.AspNetCore.App" />
```

This includes:
```
Microsoft.AspNetCore.App
├─ Microsoft.AspNetCore (core framework)
├─ Microsoft.AspNetCore.Mvc (controllers, routing)
├─ Microsoft.AspNetCore.Hosting (server, configuration)
├─ Microsoft.Extensions.Configuration (config reading)
├─ Microsoft.Extensions.DependencyInjection (DI container)
├─ Microsoft.Extensions.Logging (structured logging)
└─ System.* libraries (BCL - Base Class Library)
```

**Versions:** Determined by target framework `net6.0`

### OpenTelemetry NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `OpenTelemetry` | 1.5.0+ | Core instrumentation SDK |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol.Grpc` | 1.5.0+ | Export via OTLP gRPC to Collector |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.5.0+ | Automatic HTTP server span creation |
| `OpenTelemetry.Instrumentation.Http` | 1.5.0+ | Automatic HTTP client call spans |
| `OpenTelemetry.Extensions.Hosting` | 1.5.0+ | Integration with .NET hosting |

**Resolution Chain:**
```
Program.cs (in each service):
  AddOpenTelemetry()
    ├─ AddOtlpExporter() → connects to otel-collector:4317
    ├─ AddAspNetCoreInstrumentation() → spans for HTTP server
    └─ AddHttpClientInstrumentation() → spans for client calls
```

### CatalogService Specific Dependencies

```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
  
  <!-- OpenTelemetry -->
  <PackageReference Include="OpenTelemetry" Version="1.5.0" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol.Grpc" Version="1.5.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.5.0" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.5.0" />
  
  <!-- Optional: Testing -->
  <PackageReference Include="xunit" Version="2.5.0" />
  <PackageReference Include="Moq" Version="4.18.0" />
  <PackageReference Include="FluentAssertions" Version="6.11.0" />
</ItemGroup>
```

**Transitive Dependencies** (resolved by NuGet):
```
OpenTelemetry
├─ System.Diagnostics.DiagnosticSource (tracing infrastructure)
├─ Grpc.Core.Api (gRPC types)
└─ Google.Protobuf (protobuf serialization)

OpenTelemetry.Exporter.OpenTelemetryProtocol.Grpc
├─ Grpc.Net.Client (gRPC client)
└─ OpenTelemetry (core)

OpenTelemetry.Instrumentation.AspNetCore
├─ OpenTelemetry (core)
└─ System.Diagnostics.DiagnosticSource
```

### CartService Specific Dependencies

```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
  
  <!-- OpenTelemetry (same as above) -->
  <PackageReference Include="OpenTelemetry" Version="1.5.0" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol.Grpc" Version="1.5.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.5.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.5.0" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.5.0" />
  
  <!-- HttpClient via System.Net.Http (implicit in framework) -->
  
  <!-- Testing -->
  <PackageReference Include="xunit" Version="2.5.0" />
  <PackageReference Include="Moq" Version="4.18.0" />
</ItemGroup>
```

**Additional vs. CatalogService:**
- `OpenTelemetry.Instrumentation.Http` (for CatalogService calls)

### OrderService Specific Dependencies

```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
  
  <!-- OpenTelemetry (full set) -->
  <PackageReference Include="OpenTelemetry" Version="1.5.0" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol.Grpc" Version="1.5.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.5.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.5.0" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.5.0" />
  
  <!-- Testing -->
  <PackageReference Include="xunit" Version="2.5.0" />
  <PackageReference Include="Moq" Version="4.18.0" />
</ItemGroup>
```

**Same as CartService:** Makes multiple inter-service calls to CartService and CatalogService

---

## .NET Base Class Library Dependencies

These are implicit with target framework `net6.0`:

| Namespace | Purpose | Available Without Explicit Reference |
|-----------|---------|---|
| `System` | Core types, exceptions, attributes | ✅ Yes |
| `System.Collections.Generic` | Lists, dictionaries, sets | ✅ Yes |
| `System.Threading.Tasks` | Async/await, Task types | ✅ Yes |
| `System.Net.Http` | HttpClient, HTTP messages | ✅ Yes |
| `System.Text.Json` | JSON serialization (ASP.NET default) | ✅ Yes |
| `Microsoft.AspNetCore.Mvc` | Controllers, routing, binding | ✅ Yes (via framework ref) |
| `Microsoft.Extensions.Logging` | Structured logging | ✅ Yes (via framework ref) |
| `Microsoft.Extensions.DependencyInjection` | Dependency injection | ✅ Yes (via framework ref) |

---

## Next.js Frontend Dependencies

### Core Runtime Dependencies

```json
{
  "dependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "next": "^14.0.0"
  }
}
```

**Versions:**
- `react@18.x` — Modern React with concurrent features
- `react-dom@18.x` — React DOM rendering
- `next@14.x` — Next.js with App Router (file-based routing)

**Transitive Dependencies (selected):**
```
next
├─ react (peer dependency)
├─ react-dom (peer dependency)
├─ webpack (bundler, implicit)
├─ postcss (CSS processing)
├─ terser (JavaScript minifier)
└─ swc (Rust-based transpiler)

react
├─ react-dom (exports)
└─ scheduler (internal task scheduler)

react-dom
└─ react (peer dependency)
```

### OpenTelemetry Dependencies for Frontend

```json
{
  "dependencies": {
    "@opentelemetry/api": "^1.7.0",
    "@opentelemetry/sdk-node": "^0.43.0",
    "@opentelemetry/sdk-trace-node": "^0.43.0",
    "@opentelemetry/auto-instrumentations-node": "^0.39.4",
    "@opentelemetry/exporter-trace-otlp-grpc": "^0.43.0",
    "@opentelemetry/instrumentation-fetch": "^0.43.0",
    "@opentelemetry/instrumentation-http": "^0.43.0"
  }
}
```

| Package | Version | Purpose |
|---------|---------|---------|
| `@opentelemetry/api` | ^1.7.0 | OTel API (types, context) |
| `@opentelemetry/sdk-node` | ^0.43.0 | Node.js SDK for tracing |
| `@opentelemetry/auto-instrumentations-node` | ^0.39.4 | Automatic instrumentation plugins |
| `@opentelemetry/exporter-trace-otlp-grpc` | ^0.43.0 | Export to OTel Collector via gRPC |
| `@opentelemetry/instrumentation-fetch` | ^0.43.0 | Instrument browser fetch calls |
| `@opentelemetry/instrumentation-http` | ^0.43.0 | Instrument Node.js HTTP calls |

**Transitive Dependencies:**
```
@opentelemetry/sdk-node
├─ @opentelemetry/api (types)
├─ @opentelemetry/core (internal utilities)
├─ @opentelemetry/resources (resource detection)
└─ @opentelemetry/semantic-conventions (standard attributes)

@opentelemetry/exporter-trace-otlp-grpc
├─ @opentelemetry/api
├─ @grpc/grpc-js (gRPC client)
├─ protobufjs (protobuf serialization)
└─ @opentelemetry/core
```

### Development Dependencies

```json
{
  "devDependencies": {
    "typescript": "^5.0.0",
    "@types/react": "^18.2.0",
    "@types/react-dom": "^18.2.0",
    "@types/node": "^20.0.0",
    "eslint": "^8.40.0",
    "eslint-config-next": "^14.0.0",
    "prettier": "^3.0.0"
  }
}
```

| Package | Purpose |
|---------|---------|
| `typescript` | Type checking |
| `@types/react` | React type definitions |
| `@types/react-dom` | React DOM type definitions |
| `@types/node` | Node.js type definitions |
| `eslint` | Code linting |
| `eslint-config-next` | Next.js ESLint rules |
| `prettier` | Code formatting |

### Testing Dependencies (Optional)

```json
{
  "devDependencies": {
    "jest": "^29.0.0",
    "@testing-library/react": "^14.0.0",
    "@testing-library/jest-dom": "^6.0.0"
  }
}
```

---

## Docker Image Base Dependencies

### ASP.NET Core Services

**Build Stage:**
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:6.0
```

**Contents:**
- .NET SDK 6.0 (compiler, runtime, tools)
- Base OS: Debian 11
- Build tools: Git, curl, wget, etc.
- Size: ~2GB

**Runtime Stage:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0
```

**Contents:**
- .NET Runtime 6.0 (minimal, no SDK)
- Base OS: Debian 11 (slim variant)
- Size: ~200MB

### Next.js Frontend

**Build Stage:**
```dockerfile
FROM node:18-alpine
```

**Contents:**
- Node.js 18 LTS
- npm (package manager)
- Alpine Linux (minimal base OS)
- Size: ~200MB

**Runtime Stage:**
```dockerfile
FROM node:18-alpine
```

**Contents:**
- Node.js 18 runtime
- Alpine Linux
- Built Next.js application
- Size: ~400-500MB

---

## Container Dependency Chain

### Service Dependencies (Runtime)

```
CartService Container
  ├─ Depends on: CatalogService (network connectivity)
  │   └─ URL: http://catalog-service:5001
  ├─ Depends on: OpenTelemetry Collector (network connectivity)
  │   └─ URL: otel-collector:4317 (gRPC)
  └─ Libraries: .NET Runtime, NuGet packages

OrderService Container
  ├─ Depends on: CartService
  │   └─ URL: http://cart-service:5002
  ├─ Depends on: CatalogService
  │   └─ URL: http://catalog-service:5001
  ├─ Depends on: OpenTelemetry Collector
  │   └─ URL: otel-collector:4317
  └─ Libraries: .NET Runtime, NuGet packages

Web Frontend Container
  ├─ Depends on: CatalogService (via HTTP, calls from browser)
  ├─ Depends on: CartService (via HTTP)
  ├─ Depends on: OrderService (via HTTP)
  ├─ Depends on: OpenTelemetry Collector (gRPC, from Node.js)
  └─ Libraries: Node.js, npm packages

OpenTelemetry Collector
  ├─ Depends on: Prometheus (for exporting metrics)
  ├─ Depends on: Loki (for exporting logs)
  ├─ Depends on: Tempo (for exporting traces)
  └─ Libraries: OpenTelemetry Collector binary
```

---

## Dependency Version Constraints

### .NET Versions (Defined in .csproj)

```xml
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
  <!-- Implicitly pulls compatible NuGet package versions -->
</PropertyGroup>

<ItemGroup>
  <!-- Exact or range version specifiers -->
  <PackageReference Include="OpenTelemetry" Version="1.5.0" />
  <!-- Or with range: -->
  <PackageReference Include="OpenTelemetry" Version="[1.5.0, 1.6.0)" />
</ItemGroup>
```

**Version Format:**
- `1.5.0` — Exact version
- `1.5.*` — Match major.minor, allow any patch
- `[1.5.0, 1.6.0)` — Range (inclusive lower, exclusive upper)

### npm Versions (Defined in package.json)

```json
{
  "dependencies": {
    "react": "^18.2.0",
    "next": "~14.0.0"
  }
}
```

**Version Format:**
- `18.2.0` — Exact version
- `^18.2.0` — Allow major version bumps, caret (>= 18.2.0, < 19.0.0)
- `~14.0.0` — Allow patch bumps, tilde (>= 14.0.0, < 14.1.0)
- `*` — Any version

**Lock File:**
```
package-lock.json
  ├─ Locks all transitive dependencies to exact versions
  └─ Ensures reproducible installs across machines
```

---

## Dependency Security Considerations

### Known Vulnerabilities Check

**For .NET:**
```bash
dotnet list package --vulnerable
```

**For npm:**
```bash
npm audit
```

### Typical Security Updates

| Package | Frequency | Notes |
|---------|-----------|-------|
| .NET Runtime | Monthly | Security patches |
| ASP.NET Core | Monthly | Security patches |
| OpenTelemetry | Quarterly | Regular updates |
| Next.js | Weekly | Regular releases, security when needed |
| React | Monthly | Maintenance releases |
| npm packages | Frequent | Use `npm audit fix` |

### Recommended Practices

1. **Lock Dependency Versions**
   - Use `package-lock.json` (npm auto-creates)
   - Commit lock files to git
   - Don't use wildcard versions in production

2. **Regular Updates**
   - Monthly: Check for security updates
   - Quarterly: Update major dependencies
   - Test thoroughly before merging

3. **Automated Scanning**
   - GitHub Dependabot (auto-create PRs for updates)
   - `npm audit` (in CI pipeline)
   - `dotnet list package --vulnerable` (in CI pipeline)

---

## Dependency Size Impact

### Binary Sizes

| Component | Compiled Size | Runtime Size |
|-----------|---|---|
| CatalogService.dll | ~200KB | ~200KB (loaded) |
| CartService.dll | ~250KB | ~250KB |
| OrderService.dll | ~250KB | ~250KB |
| Next.js bundle (.next) | ~800KB | Cached in browser |

### Transitive Dependency Count

**NuGet Packages (per .NET service):**
```
Direct: 5 packages (OpenTelemetry + extensions)
Transitive: ~40-50 total packages resolved
```

**npm Packages (Next.js frontend):**
```
Direct: 20-30 packages
Transitive: 300-500 total (React ecosystem is large)
```

### Download Times (Approximate)

**Initial Setup (first clone):**
```
dotnet restore (all .NET services): ~2-3 minutes
npm install (frontend): ~5-10 minutes
docker pull (all images): ~10-15 minutes
Total: ~20-30 minutes on typical internet
```

**Rebuild (with cache):**
```
dotnet build: ~30 seconds (per service)
npm run build: ~60 seconds
docker compose up: ~30 seconds (if images cached)
```

---

## Dependency Upgrade Strategy

### Semantic Versioning

All dependencies follow semver: `MAJOR.MINOR.PATCH`

```
1.5.3
│ │ └─ Patch: Bug fixes (safe to update)
│ └─── Minor: New features, backward compatible (usually safe)
└───── Major: Breaking changes (requires testing)
```

### Recommended Upgrade Order

1. **Patch Updates** (automatic)
   - `npm audit fix` (fixes security issues)
   - Safe to apply immediately

2. **Minor Updates** (monthly)
   - `npm update` (respects version constraints)
   - Test integration scenarios
   - Low risk

3. **Major Updates** (quarterly)
   - Manual update of package.json
   - Review breaking changes
   - Full test suite required
   - High scrutiny

### Version Pinning Strategy

**For Learning Project:**
```json
{
  "dependencies": {
    "react": "^18.2.0",          // Allow minor updates
    "next": "^14.0.0",           // Allow minor updates
    "@opentelemetry/api": "^1.7.0"
  }
}
```

**For Production:**
```json
{
  "dependencies": {
    "react": "18.2.15",          // Exact version
    "next": "14.0.3",            // Exact version
    "@opentelemetry/api": "1.7.0"
  }
}
```

---

## Dependency Compatibility Matrix

| Service | Requires | Provides | Compatible With |
|---------|----------|----------|---|
| CartService | CatalogService | HTTP API | REST clients |
| OrderService | CartService + CatalogService | HTTP API | REST clients |
| Web Frontend | All three services | Web UI | Any browser |
| All Services | OpenTelemetry Collector | Telemetry | OTLP-compliant backends |
| OTel Collector | Prometheus, Loki, Tempo | Telemetry | Grafana, any TSDB/log store |

### Backward Compatibility

**Within Major Version:**
- `.NET 6.0` → `.NET 6.1` → `.NET 6.2`: Backward compatible
- Updating patch versions is safe

**Across Major Versions:**
- `.NET 6.0` → `.NET 7.0`: May require code changes
- OpenTelemetry 1.x → 2.x: API changes expected

---

## Dependency Documentation

Each service includes:
- `README.md` — Setup and dependency instructions
- `Dockerfile` — Container dependencies listed
- `docker-compose.yml` — Service interdependencies declared
- `.csproj` or `package.json` — Direct dependencies listed
- Lock files (`package-lock.json`) — All transitive dependencies

To view all dependencies:
```bash
# .NET services
cd apps/services/CartService
dotnet list package --all

# Next.js frontend
cd apps/web
npm ls
```
