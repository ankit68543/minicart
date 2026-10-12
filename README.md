# 🛒 MiniCart — AI-Driven Shopping Cart Application

Learn **AIDLC workflow**, **.NET microservices**, **OpenTelemetry observability**, and **Docker Compose** by building a complete, production-ready shopping cart application.

## 📚 Project Overview

**MiniCart** is a learning project that teaches:
- ✅ AWS Labs AIDLC workflow (4 phases: Ideation → Inception → Construction → Evaluation)
- ✅ Microservices architecture (3 independent .NET Core APIs)
- ✅ Modern frontend (Next.js + React + TypeScript)
- ✅ Complete observability (OpenTelemetry + Prometheus + Loki + Tempo + Grafana)
- ✅ Docker Compose orchestration (2 separated networks)

---

## 🏗️ Architecture at a Glance

### **Two Docker Compose Networks**

```
┌─────────────────────────────┐
│     app Network             │
│  (Application Services)     │
├─────────────────────────────┤
│ • Web (Next.js frontend)    │
│ • CatalogService (.NET API) │
│ • CartService (.NET API)    │
│ • OrderService (.NET API)   │
└─────────────────────────────┘
           ↓ (via bridge)
┌─────────────────────────────┐
│  observability Network      │
│  (Monitoring & Analytics)   │
├─────────────────────────────┤
│ • OpenTelemetry Collector   │
│ • Prometheus (metrics)      │
│ • Loki (logs)               │
│ • Tempo (traces)            │
│ • Grafana (dashboards)      │
└─────────────────────────────┘
```

### **Why Two Networks?**

- **app network**: Contains only application services. Clean, focused, minimal exposure.
- **observability network**: Contains only monitoring services. Prevents accidental direct access to metrics/logs from app services.
- **Bridge connection**: Services on the `app` network send telemetry to the OTel Collector, which lives on the `observability` network. Clean separation of concerns.

---

## 📂 Repository Structure

```
minicart/
├── apps/
│   ├── web/                          # Next.js frontend
│   │   ├── app/                      # Next.js App Router
│   │   ├── components/               # React components
│   │   ├── lib/                      # Utility functions (API clients, etc.)
│   │   ├── instrumentation.ts        # OpenTelemetry setup
│   │   └── package.json
│   │
│   └── services/                     # .NET Core microservices
│       ├── CatalogService/           # Product catalog
│       │   ├── Program.cs
│       │   ├── Models/
│       │   ├── Controllers/
│       │   ├── Services/
│       │   └── CatalogService.csproj
│       │
│       ├── CartService/              # Shopping cart management
│       │   └── [same structure]
│       │
│       └── OrderService/             # Order processing
│           └── [same structure]
│
├── observability/
│   ├── otel-collector/               # OpenTelemetry Collector config
│   ├── prometheus/                   # Prometheus config & dashboards
│   ├── loki/                         # Loki log aggregation config
│   ├── tempo/                        # Tempo trace backend config
│   └── grafana/                      # Grafana provisioning
│
├── tests/                            # Integration & e2e tests
├── scripts/                          # Deployment & helper scripts
├── docs/                             # Architecture & learning guides
│
├── docker-compose.yml                # Orchestration (app + observability)
├── .env.example                      # Environment variables template
└── README.md                         # This file
```

---

## 🚀 Quick Start

### Prerequisites

- **Windows 11** with **WSL 2** (Ubuntu 22.04 LTS)
- **Docker Engine** running in WSL 2 (no Docker Desktop required)
- **.NET SDK 5.0+**
- **Node.js 18+**
- **Git**

### 1. Clone and Setup

```bash
cd D:\OTelLearning\minicart
git config user.email "you@example.com"
git config user.name "Your Name"
git add .
git commit -m "Initial project structure"
```

### 2. Start the AIDLC Workflow

Open **GitHub Copilot** or **VS Code Agent Mode** and run:

```
/aidlc Build a shopping cart microservices application using AIDLC workflow, 
  with Next.js frontend, three .NET Core APIs (Catalog, Cart, Order), 
  complete OpenTelemetry observability, and two Docker Compose networks.
```

The AIDLC orchestrator will guide you through:
- **Phase 1 (IDEATION)**: Clarify requirements
- **Phase 2 (INCEPTION)**: Design architecture
- **Phase 3 (CONSTRUCTION)**: Build and test
- **Phase 4 (EVALUATION)**: Verify quality

### 3. Run the Application

```bash
# Build all services
docker compose build

# Start everything
docker compose up -d

# View logs
docker compose logs -f

# Access the application
# Frontend: http://localhost:3000
# Grafana: http://localhost:3000 (use AIDLC to set this up)
```

---

## 📖 Learning Path

### Phase 1: IDEATION (Requirements)
- **Duration**: 15-30 minutes
- **Task**: Understand what we're building
- **Output**: Scope document, user stories, success criteria
- **Gate**: Is the scope clear and feasible?

### Phase 2: INCEPTION (Architecture)
- **Duration**: 30-45 minutes
- **Task**: Design the system
- **Output**: Architecture diagrams, network design, units of work
- **Gate**: Is the design sound?

### Phase 3: CONSTRUCTION (Implementation)
- **Duration**: 2-3 hours
- **Task**: Build all services incrementally
- **Output**: Complete code, tests passing, no errors
- **Gate**: Do all tests pass? Is the build successful?

### Phase 4: EVALUATION (Quality)
- **Duration**: 30-45 minutes
- **Task**: Verify everything works
- **Output**: Quality report, troubleshooting guide
- **Gate**: Does it meet all requirements?

---

## 🔍 Key Concepts Explained

### Docker Networks

**Why separate networks?**
- Microservices should only communicate with each other, not directly with monitoring tools.
- Monitoring tools should not be directly accessible from application services.
- This prevents accidental data leaks and enforces clean architecture.

**How telemetry flows:**
1. Services (in `app` network) emit traces, metrics, and logs.
2. OpenTelemetry Collector (on both networks) receives telemetry.
3. Collector exports to Prometheus, Loki, and Tempo (all on `observability` network).
4. Grafana (on `observability` network) queries the backends and displays dashboards.

### OpenTelemetry Instrumentation

All three .NET APIs will be instrumented with:
- **Traces**: Distributed tracing with W3C Trace Context propagation
- **Metrics**: Request count, duration, errors, health
- **Logs**: Structured JSON logs with trace correlation

Next.js will instrument:
- **Server-side traces**: API requests to the .NET services
- **Metrics**: Frontend performance (optional)
- **Logs**: Application events with trace context

---

## 📊 Observability Stack

| Component | Purpose | Port |
|-----------|---------|------|
| **OpenTelemetry Collector** | Receives and routes telemetry | 4317 (OTLP gRPC), 4318 (HTTP) |
| **Prometheus** | Metrics aggregation & storage | 9090 |
| **Loki** | Log aggregation & querying | 3100 |
| **Tempo** | Distributed trace storage | 3200 |
| **Grafana** | Dashboards & visualization | 3000 |

---

## 🎯 Success Criteria

By the end of this project, you will have:

✅ A working MiniCart application (frontend + 3 APIs)  
✅ A functioning docker-compose setup with two networks  
✅ Complete instrumentation with OpenTelemetry  
✅ Prometheus metrics (request count, latency, errors)  
✅ Loki logs with trace correlation  
✅ Tempo distributed traces  
✅ Grafana dashboards showing real-time observability  
✅ Automated tests for all services  
✅ Failure scenario demonstrations (and recovery)  
✅ A complete audit trail from AIDLC workflow  

---

## 🛠️ Tools & Technologies

| Category | Tools |
|----------|-------|
| **Frontend** | Next.js, React, TypeScript, Tailwind CSS |
| **Backend** | ASP.NET Core 5.0+, C#, xUnit |
| **Container** | Docker, Docker Compose |
| **Observability** | OpenTelemetry SDK, Prometheus, Loki, Tempo, Grafana |
| **Workflow** | AWS Labs AIDLC 2.11.0, GitHub Copilot Agent Mode |

---

## 📝 Next Steps

1. **Read this README** (you're doing it!)
2. **Open VS Code** in this directory
3. **Use GitHub Copilot Agent Mode** and run `/aidlc` command
4. **Follow the AIDLC workflow** through all 4 phases
5. **Build incrementally** — don't try to do everything at once

---

## 📚 Resources

- [AWS Labs AIDLC Repository](https://github.com/awslabs/aidlc-workflows)
- [AIDLC Getting Started Guide](../aidlc-official/docs/guide/01-getting-started.md)
- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
- [Docker Compose Networks](https://docs.docker.com/compose/networking/)

---

## 🎓 Learning Goals

### By the end of this project, you will understand:

- ✅ **AIDLC Lifecycle**: How AI-driven development workflows work
- ✅ **Microservices**: Communication, independence, scalability
- ✅ **Observability**: Why it matters and how to implement it
- ✅ **Docker Compose**: Multi-service orchestration and networking
- ✅ **OpenTelemetry**: Traces, metrics, logs, correlation
- ✅ **Testing**: Automated quality gates and guardrails
- ✅ **.NET Core**: Building production-grade APIs
- ✅ **Next.js**: Modern frontend development

---

**Ready to learn? Open GitHub Copilot Agent Mode and type `/aidlc`!**
