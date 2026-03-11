---
phase: 3
slug: vue-spa-oidc
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-03-11
---

# Phase 3 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | Playwright (e2e browser smoke tests) |
| **Config file** | `simple-admin-spa/playwright.config.ts` — Wave 0 installs |
| **Quick run command** | `npx playwright test --project=chromium login.spec.ts` |
| **Full suite command** | `npx playwright test` |
| **Estimated runtime** | ~30 seconds |

---

## Sampling Rate

- **After every task commit:** Manual browser verification (SPA not unit-testable at task granularity)
- **After every plan wave:** Run `npx playwright test --project=chromium`
- **Before `/gsd:verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 03-01-01 | 01 | 1 | AUTH-02 | e2e smoke | `npx playwright test --project=chromium -g "unauthenticated redirect"` | ❌ W0 | ⬜ pending |
| 03-01-02 | 01 | 1 | AUTH-02 | e2e smoke | `npx playwright test --project=chromium -g "login flow"` | ❌ W0 | ⬜ pending |
| 03-02-01 | 02 | 1 | AUTH-02 | e2e smoke | `npx playwright test --project=chromium -g "session persistence"` | ❌ W0 | ⬜ pending |
| 03-02-02 | 02 | 1 | AUTH-02 | e2e smoke | `npx playwright test --project=chromium -g "logout"` | ❌ W0 | ⬜ pending |
| 03-02-03 | 02 | 1 | AUTH-02 | e2e smoke | `npx playwright test --project=chromium -g "protected route after logout"` | ❌ W0 | ⬜ pending |
| 03-02-04 | 02 | 1 | AUTH-02 | e2e smoke | `npx playwright test --project=chromium -g "api bearer"` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*

---

## Wave 0 Requirements

- [ ] `simple-admin-spa/playwright.config.ts` — Playwright config, baseURL `http://localhost:5173`
- [ ] `simple-admin-spa/tests/auth.spec.ts` — stubs for all AUTH-02 success criteria
- [ ] Framework install: `npm install @playwright/test -D && npx playwright install chromium` in SPA project

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| PrimeVue theme renders correctly | N/A (visual) | Visual verification only | Open SPA, check toolbar and layout render with Aura theme |

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
