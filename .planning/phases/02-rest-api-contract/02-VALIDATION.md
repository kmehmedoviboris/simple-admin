---
phase: 2
slug: rest-api-contract
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-11
---

# Phase 2 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit 2.9.3 + Microsoft.AspNetCore.Mvc.Testing 10.0.4 |
| **Config file** | SimpleAdmin.Tests/SimpleAdmin.Tests.csproj |
| **Quick run command** | `dotnet test --filter "Category=Smoke"` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~15 seconds |

---

## Sampling Rate

- **After every task commit:** Run `dotnet test --filter "Category=Smoke"`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 15 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 02-01-01 | 01 | 1 | USER-01 | integration | `dotnet test --filter "FullyQualifiedName~UsersControllerTests.GetAll"` | ❌ W0 | ⬜ pending |
| 02-01-02 | 01 | 1 | USER-02 | integration | `dotnet test --filter "FullyQualifiedName~UsersControllerTests.Create"` | ❌ W0 | ⬜ pending |
| 02-01-03 | 01 | 1 | USER-03 | integration | `dotnet test --filter "FullyQualifiedName~UsersControllerTests.Update"` | ❌ W0 | ⬜ pending |
| 02-01-04 | 01 | 1 | USER-04 | integration | `dotnet test --filter "FullyQualifiedName~UsersControllerTests.Delete"` | ❌ W0 | ⬜ pending |
| 02-02-01 | 02 | 1 | USER-01 | integration (smoke) | `dotnet test --filter "Category=Smoke"` | ❌ W0 | ⬜ pending |
| 02-02-02 | 02 | 1 | (all) | integration | `dotnet test --filter "FullyQualifiedName~UsersControllerTests.OpenApi"` | ❌ W0 | ⬜ pending |
| 02-02-03 | 02 | 1 | (all) | integration (smoke) | `dotnet test --filter "FullyQualifiedName~UsersControllerTests.Unauthorized"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `SimpleAdmin.Tests/UsersControllerTests.cs` — stubs for USER-01 through USER-04
- [ ] `SimpleAdmin.Tests/Helpers/TokenHelper.cs` — shared PKCE token acquisition helper

*Existing infrastructure covers test framework and factory: `TestWebApplicationFactory.cs`, `CookieTrackingHandler.cs`*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Scalar UI renders at /scalar | (all) | Visual rendering | Navigate to /scalar in browser; confirm endpoints listed |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 15s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
