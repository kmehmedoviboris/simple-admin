---
phase: 4
slug: user-crud-views
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-11
---

# Phase 4 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright 1.58.2 |
| **Config file** | `simple-admin-spa/playwright.config.ts` |
| **Quick run command** | `cd simple-admin-spa && npx playwright test tests/auth.spec.ts` |
| **Full suite command** | `cd simple-admin-spa && npx playwright test` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Run `cd simple-admin-spa && npx playwright test tests/auth.spec.ts`
- **After every plan wave:** Run `cd simple-admin-spa && npx playwright test`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 04-01-01 | 01 | 1 | UX-02 | e2e | `npx playwright test tests/crud.spec.ts --grep "loading"` | ❌ W0 | ⬜ pending |
| 04-01-02 | 01 | 1 | UX-03 | e2e | `npx playwright test tests/crud.spec.ts --grep "error toast"` | ❌ W0 | ⬜ pending |
| 04-01-03 | 01 | 1 | UX-03 | e2e | `npx playwright test tests/crud.spec.ts --grep "delete"` | ❌ W0 | ⬜ pending |
| 04-02-01 | 02 | 1 | UX-01 | e2e | `npx playwright test tests/crud.spec.ts --grep "validation"` | ❌ W0 | ⬜ pending |
| 04-02-02 | 02 | 1 | UX-02 | e2e | `npx playwright test tests/crud.spec.ts --grep "loading"` | ❌ W0 | ⬜ pending |
| 04-02-03 | 02 | 1 | UX-03 | e2e | `npx playwright test tests/crud.spec.ts --grep "error toast"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `simple-admin-spa/tests/crud.spec.ts` — e2e tests for UX-01, UX-02, UX-03 (list view, create, edit, delete, validation, toasts)
- [ ] Extract `loginAsAdmin` helper from `tests/auth.spec.ts` to shared `tests/helpers.ts` or copy inline

*Existing `tests/auth.spec.ts` covers auth smoke — remains unchanged.*

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Toast auto-dismiss after 3s | UX-03 | Timing-sensitive, flaky in CI | 1. Trigger an API error 2. Observe toast appears top-right 3. Wait 3s, confirm it auto-dismisses |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
