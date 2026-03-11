# Requirements: SimpleAdmin

**Defined:** 2026-03-11
**Core Value:** A working end-to-end auth code flow login that lands in a Vue 3 SPA where you can list, create, edit, and delete users.

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Authentication

- [x] **AUTH-01**: User can log in via Authorization Code + PKCE flow through a Razor-rendered login page
- [x] **AUTH-02**: User can log out and is redirected to the login page with tokens cleared
- [x] **AUTH-03**: All API requests include a Bearer token; unauthenticated requests return 401

### User Management

- [x] **USER-01**: User can view a list of all users in a PrimeVue DataTable
- [x] **USER-02**: User can create a new user by providing email and password
- [x] **USER-03**: User can edit an existing user's email and optionally change their password
- [x] **USER-04**: User can delete a user after confirming via a confirmation dialog

### UX Foundations

- [ ] **UX-01**: Forms display inline validation errors for required fields and email format
- [ ] **UX-02**: List view and form submissions show loading/busy indicators
- [ ] **UX-03**: API failures (4xx/5xx) display toast error messages to the user

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### User Management Enhancements

- **USER-05**: User can search/filter the user list by email
- **USER-06**: User can sort user list columns
- **USER-07**: User list supports server-side pagination
- **USER-08**: User can toggle a user's active/inactive status

### Advanced Features

- **ADV-01**: Role management UI for assigning roles to users
- **ADV-02**: Audit log of user management actions
- **ADV-03**: Bulk delete of multiple users

## Out of Scope

| Feature | Reason |
|---------|--------|
| Role management UI | Not needed for POC; roles stored by Identity but no UI |
| User profile / avatar | No file storage; email-only identity sufficient |
| Dashboard / analytics | No meaningful metrics for in-memory POC |
| Mobile responsiveness | Desktop-only POC |
| Password reset / email verification | No email infrastructure for POC |
| Two-factor authentication | Local-only POC, single-factor sufficient |
| Persistent database | In-memory EF Core is the stated constraint |
| Real-time updates | No WebSocket/SSE needed for POC |
| CI/CD / deployment | Local development only |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| AUTH-01 | Phase 1 | Complete |
| AUTH-02 | Phase 3 | Complete |
| AUTH-03 | Phase 1 | Complete |
| USER-01 | Phase 2 | Complete |
| USER-02 | Phase 2 | Complete |
| USER-03 | Phase 2 | Complete |
| USER-04 | Phase 2 | Complete |
| UX-01 | Phase 4 | Pending |
| UX-02 | Phase 4 | Pending |
| UX-03 | Phase 4 | Pending |

**Coverage:**
- v1 requirements: 10 total
- Mapped to phases: 10
- Unmapped: 0

---
*Requirements defined: 2026-03-11*
*Last updated: 2026-03-11 after roadmap creation*
