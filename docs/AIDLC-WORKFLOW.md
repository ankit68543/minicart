# 🚀 AIDLC Workflow Guide for MiniCart

This guide explains what happens in each AIDLC phase and how to navigate the workflow using GitHub Copilot Agent Mode.

---

## 📋 The Four AIDLC Phases

### Phase 1: IDEATION (Requirements & Scope)
**Duration:** 15-30 minutes  
**Role:** Analyst  
**Goal:** Understand what we're building  
**Approval Gate:** Is the scope clear and feasible?

**What happens:**
1. You describe the project requirements
2. AIDLC (via Copilot) asks clarifying questions
3. Together, you define:
   - User stories (concrete examples of usage)
   - Success criteria (how to know it works)
   - Scope boundaries (what we're NOT building)
   - Identified risks

**Output:** `PROJECT-SCOPE.md` artifact

**Key Questions:**
- What does a user do in the app? (Add products, checkout, view order history)
- What's the success criteria? (All services running, observability working)
- What are we NOT building? (Real payments, authentication, databases)

---

### Phase 2: INCEPTION (Architecture & Design)
**Duration:** 30-45 minutes  
**Role:** Architect  
**Goal:** Design how we'll build it  
**Approval Gate:** Is the architecture sound?

**What happens:**
1. AIDLC reviews the scope from Phase 1
2. Architect designs:
   - System architecture (services, communication)
   - Docker network design (`app` and `observability`)
   - APIs and data models
   - Technology choices and justification

3. Breaks work into **units of work** (6-8 small, independent tasks)
   - Example: "Build CatalogService API skeleton with health endpoint"
   - Example: "Instrument CartService with OpenTelemetry"
   - Example: "Create Grafana dashboard for request latency"

4. Defines acceptance criteria for each unit (how to test it)

**Output:** `PROJECT-ARCHITECTURE.md` artifact + unit breakdown

**Key Deliverables:**
- Architecture diagrams (already created in `docs/ARCHITECTURE.md`)
- Service APIs (endpoints, request/response schemas)
- Network topology (two networks with bridge via OTel Collector)
- Unit of work list with dependencies

---

### Phase 3: CONSTRUCTION (Implementation & Testing)
**Duration:** 2-3 hours  
**Role:** Developer  
**Goal:** Build everything and make tests pass  
**Approval Gate:** Do all tests pass? Zero build errors?

**What happens:**
1. AIDLC presents one unit of work at a time
2. You implement the code locally
3. Write tests (Jest for Next.js, xUnit for .NET)
4. Run tests (must pass)
5. Verify against acceptance criteria
6. Move to next unit

**For each unit:**
```
1. Read unit requirements
2. Design implementation (locally)
3. Write code
4. Write tests
5. Run tests (passing = success)
6. Verify acceptance criteria
7. Commit to git
8. Move to next unit
```

**Quality Gates (automated):**
- ✅ TypeScript: 0 errors
- ✅ ESLint: 0 violations
- ✅ Jest tests: 100% passing
- ✅ .NET build: succeeds without warnings
- ✅ xUnit tests: 100% passing

**Output:** Complete working code, all tests passing

---

### Phase 4: EVALUATION (Quality Assurance & Verification)
**Duration:** 30-45 minutes  
**Role:** Evaluator/QA  
**Goal:** Verify it meets all requirements  
**Approval Gate:** Does it work? Do all tests pass?

**What happens:**
1. Run all automated tests
2. Verify against original requirements (from Phase 1)
3. Demonstrate failure scenarios:
   - A service crashes → How does monitoring detect it?
   - A request is slow → How does it appear in metrics?
   - Invalid checkout → How does it appear in logs/traces?
   - OTel Collector is down → How do we detect it?
4. Verify observability end-to-end:
   - Prometheus has metrics
   - Loki has logs
   - Tempo has traces
   - Grafana dashboards show data
5. Document everything in evaluation report

**Output:** `PROJECT-EVALUATION.md` + demonstration recordings

---

## 🎯 How to Use GitHub Copilot Agent Mode

### Starting the Workflow

1. **Open GitHub Copilot in VS Code**
   - Press `Ctrl+Shift+A` (or `Cmd+Shift+A` on Mac)
   - Or click the Copilot Chat icon in the sidebar

2. **Invoke AIDLC**
   ```
   /aidlc Build MiniCart: a shopping cart microservices application with
     Next.js frontend, three ASP.NET Core APIs (Catalog, Cart, Order),
     complete OpenTelemetry observability, and Docker Compose with two
     separate networks (app and observability).
   ```

3. **AIDLC will respond:**
   - Detect that you need to start with IDEATION
   - Show you the workflow diagram
   - Ask clarifying questions about scope, success criteria, risks

4. **Answer the prompts**
   - Be specific: "Three .NET APIs means independent services"
   - Mention constraints: "In-memory data only, no databases"
   - Clarify success: "All services healthy, observability working end-to-end"

5. **Request Gate Approval**
   - After finishing a phase, ask: `/aidlc gate approval`
   - AIDLC will check if all requirements are met
   - If approved, move to next phase

### During Each Phase

**In IDEATION:**
```
/aidlc What are the user stories for MiniCart?
/aidlc What's our success criteria?
/aidlc What are we NOT building?
/aidlc Is the scope clear? Request gate approval.
```

**In INCEPTION:**
```
/aidlc Design the architecture with two Docker networks
/aidlc Show me the units of work breakdown
/aidlc What's the API specification for each service?
/aidlc Is the design sound? Request gate approval.
```

**In CONSTRUCTION:**
```
/aidlc What's the first unit of work?
/aidlc Implement CatalogService API
/aidlc Write tests for the API
/aidlc Next unit?
/aidlc Are all tests passing? Request gate approval.
```

**In EVALUATION:**
```
/aidlc Run the quality gates
/aidlc Demonstrate: What happens if a service crashes?
/aidlc Show me observability end-to-end
/aidlc Ready for final approval?
```

---

## 🔄 AIDLC Workflow Diagram

```
┌──────────────┐
│ START HERE   │
│ /aidlc       │
└──────┬───────┘
       │
       ▼
┌──────────────────────────┐
│   IDEATION               │
│ - Clarify requirements   │
│ - Define scope           │
│ - Identify risks         │
│ - Success criteria       │
└──────────┬───────────────┘
           │
           ▼
       🚪 GATE 1
       "Is scope clear?"
       ├─ ✅ APPROVE → Continue
       └─ ❌ REVISE → Back to IDEATION
           │
           ▼
┌──────────────────────────┐
│   INCEPTION              │
│ - Design architecture    │
│ - Define APIs            │
│ - Network design         │
│ - Break into units       │
└──────────┬───────────────┘
           │
           ▼
       🚪 GATE 2
       "Is design sound?"
       ├─ ✅ APPROVE → Continue
       └─ ❌ REVISE → Back to INCEPTION
           │
           ▼
┌──────────────────────────┐
│   CONSTRUCTION           │
│ - Implement per unit     │
│ - Write tests            │
│ - Run guardrails         │
│ - Deploy to Compose      │
└──────────┬───────────────┘
           │
           ▼
       🚪 GATE 3
       "All tests pass?"
       ├─ ✅ APPROVE → Continue
       └─ ❌ FIX ISSUES → Back to CONSTRUCTION
           │
           ▼
┌──────────────────────────┐
│   EVALUATION             │
│ - Verify requirements    │
│ - Test failure scenarios │
│ - Check observability    │
│ - Generate report        │
└──────────┬───────────────┘
           │
           ▼
       🚪 GATE 4
       "Meets all requirements?"
       ├─ ✅ APPROVE → DONE ✅
       └─ ❌ ISSUES FOUND → Fix & return
```

---

## 🛠️ Approval Gate Checklist

### Gate 1: IDEATION Approval
- [ ] Scope document is complete
- [ ] User stories defined (at least 3-5 examples)
- [ ] Success criteria are measurable
- [ ] Risks identified and mitigation planned
- [ ] Technologies decided (Next.js, .NET, Docker, OTel, etc.)
- [ ] Scope boundaries clear (what we're NOT building)

### Gate 2: INCEPTION Approval
- [ ] Architecture diagrams created
- [ ] Network design documented (two networks)
- [ ] APIs specified (endpoints, schemas)
- [ ] 6-8 units of work defined
- [ ] Acceptance criteria for each unit
- [ ] Dependencies between units identified
- [ ] Technology justification provided

### Gate 3: CONSTRUCTION Approval
- [ ] All units of work completed
- [ ] All tests passing (100%)
- [ ] Zero TypeScript errors
- [ ] Zero ESLint violations
- [ ] Docker Compose builds without errors
- [ ] All services start and are healthy
- [ ] Observability working end-to-end

### Gate 4: EVALUATION Approval
- [ ] All requirements verified
- [ ] Failure scenarios documented
- [ ] Observability demonstrated
- [ ] Quality report generated
- [ ] Troubleshooting guide created
- [ ] README updated

---

## 📝 Important Notes

### Approval Gates are Strict
- AIDLC won't let you advance without meeting ALL gate criteria
- This prevents downstream problems
- If a gate fails, don't skip ahead — fix the issues and resubmit

### Context Reduction Between Phases
- AIDLC extracts ONLY essential information between phases
- Removes analyst reasoning, architect alternatives, etc.
- Reduces hallucination and saves tokens
- You see a summary of what passes to the next phase

### Audit Trail
- Every decision logged in JSON
- Every test result captured
- Every gate approval recorded
- See `demo-logs/audit-trail.jsonl` for example

### Artifacts are Versioned
- Each phase produces a versioned artifact
- Artifacts are committed to git
- Complete audit trail of project evolution

---

## 🎓 Learning Outcomes

By the end of this workflow, you'll understand:

✅ **AIDLC Methodology** — How AI-driven development workflow works  
✅ **Approval Gates** — Why structured checkpoints improve quality  
✅ **Context Reduction** — How to hand off work between roles  
✅ **Microservices** — Communication, independence, testing  
✅ **Observability** — Why it matters and how to measure it  
✅ **Docker Compose** — Orchestration and networking  
✅ **OpenTelemetry** — Traces, metrics, logs, correlation  
✅ **.NET Core** — Building production APIs  
✅ **Next.js** — Modern frontend development  
✅ **Guardrails** — Automated quality checks  

---

## 🆘 If You Get Stuck

### "I'm not sure what to do next"
→ Ask: `/aidlc What's the next step in this phase?`

### "A test is failing"
→ Ask: `/aidlc Help me debug this test failure`

### "I don't understand the requirement"
→ Ask: `/aidlc Explain this unit of work in simpler terms`

### "The gate didn't approve"
→ Ask: `/aidlc What do I need to do to pass this gate?`

### "I want to see an example"
→ Ask: `/aidlc Show me an example of a working CatalogService`

---

**Ready? Open GitHub Copilot and type `/aidlc` to begin!**
