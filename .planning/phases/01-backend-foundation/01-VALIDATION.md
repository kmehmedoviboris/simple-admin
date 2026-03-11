---
phase: 1
slug: backend-foundation
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-11
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9+ with `Microsoft.AspNetCore.Mvc.Testing` |
| **Config file** | `SimpleAdmin.Tests/SimpleAdmin.Tests.csproj` — Wave 0 creation |
| **Quick run command** | `dotnet test --filter "Category=Smoke" --no-build` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~10 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "Category=Smoke" --no-build`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 10 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 01-01-01 | 01 | 1 | AUTH-01 | integration | `dotnet test --filter "FullyQualifiedName~AuthFlowSmokeTests"` | ❌ W0 | ⬜ pending |
| 01-01-02 | 01 | 1 | AUTH-01 | integration | `dotnet test --filter "FullyQualifiedName~TokenEndpointTests"` | ❌ W0 | ⬜ pending |
| 01-01-03 | 01 | 1 | AUTH-03 | integration | `dotnet test --filter "FullyQualifiedName~ProtectedEndpointTests"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `SimpleAdmin.Tests/SimpleAdmin.Tests.csproj` — xUnit + `Microsoft.AspNetCore.Mvc.Testing` project
- [ ] `SimpleAdmin.Tests/AuthFlowSmokeTests.cs` — covers AUTH-01 redirect and code exchange
- [ ] `SimpleAdmin.Tests/ProtectedEndpointTests.cs` — covers AUTH-03 200/401 scenarios
- [ ] `SimpleAdmin.Tests/Helpers/TestWebApplicationFactory.cs` — `WebApplicationFactory<Program>` with test-specific seeder

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Browser authorize → Razor login page redirect | AUTH-01 | Full browser redirect chain with cookies | Navigate to `/connect/authorize?...` in browser, verify login page renders |
| Login form submit with valid credentials | AUTH-01 | Cookie-based POST with anti-forgery | Submit form in browser, verify redirect back with code |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
