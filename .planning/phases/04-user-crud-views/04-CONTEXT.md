# Phase 4: User CRUD Views - Context

**Gathered:** 2026-03-11
**Status:** Ready for planning

<domain>
## Phase Boundary

A fully working admin UI where an authenticated user can list, create, edit, and delete users — with form validation, loading indicators, and error feedback on every interaction. Uses PrimeVue DataTable, forms, ConfirmDialog, and Toast components. Builds on the existing Vue 3 SPA shell and typed API client from Phase 3.

</domain>

<decisions>
## Implementation Decisions

### Form navigation
- Separate pages for create and edit: `/users/new` and `/users/:id/edit`
- Each form is a full page with its own route (not modals or drawers)
- After successful save (create or edit), navigate back to `/users` list
- Shared `UserForm.vue` component used by both `UserCreateView.vue` and `UserEditView.vue`
- Create passes empty data; edit pre-fills from existing user
- Password field is required on create, optional on edit
- Page heading: "Create User" or "Edit User" at the top of each form page

### Table layout
- DataTable columns: Email + Actions (no emailConfirmed column — not meaningful for POC)
- Actions column: inline Edit and Delete icon buttons on each row
- "Create User" button above the table, right-aligned
- Loading spinner visible while the user list API request is in flight
- Prevent self-delete: disable delete button on the row matching the currently logged-in user (use `/api/me` to identify)

### Validation rules
- Validation triggers on submit only (not on blur or keystroke)
- Email field: required + basic email format validation (contains @ and domain)
- Password field on create: required only (no length or complexity rules client-side)
- Password field on edit: optional (leave blank to keep current password)
- Inline error messages displayed under each invalid field
- Server-side validation errors (duplicate email, password policy) shown as toast only — no inline mapping of server errors

### Error & success feedback
- PrimeVue Toast for both success and error messages
- Success toasts: green, for create ("User created"), edit ("User updated"), and delete ("User deleted")
- Error toasts: red, for API failures (4xx/5xx)
- Toast position: top-right
- Auto-dismiss after 3 seconds for all toasts
- 401 errors still trigger redirect to login (carried forward from Phase 3 — no toast for 401)

### Claude's Discretion
- PrimeVue component variants and styling choices
- Loading indicator implementation (spinner vs skeleton vs overlay)
- Exact icon choices for edit/delete buttons
- Form field layout (stacked, grid, card wrapper)
- How to identify current user for self-delete prevention (token claims vs /api/me call)

</decisions>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AppShell.vue`: PrimeVue Toolbar with "SimpleAdmin" title and Logout button — wraps all authenticated views
- `UsersView.vue`: Placeholder at `/users` — will be replaced with DataTable list view
- Generated API client (`sdk.gen.ts`): `getApiUsers`, `postApiUsers`, `putApiUsersById`, `deleteApiUsersById`, `getApiMe`
- Typed DTOs (`types.gen.ts`): `CreateUserDto` (email, password), `UpdateUserDto` (email, newPassword), `UserListDto` (id, email, emailConfirmed), `ProblemDetails`
- `authStore.ts`: Pinia store with auth state, login/logout, token management

### Established Patterns
- Vue 3 Composition API with `<script setup lang="ts">`
- PrimeVue components imported individually (e.g., `import Button from 'primevue/button'`)
- Router uses lazy-loaded components via `() => import(...)`
- Auth guard in `router/index.ts` — routes without `meta.public` require authentication
- hey-api client with Bearer token interceptor and 401 redirect handling

### Integration Points
- Router (`router/index.ts`): Add routes for `/users/new` and `/users/:id/edit`
- `AppShell.vue`: Already wraps authenticated routes — new views will inherit the shell
- API client: All CRUD operations available as typed SDK functions
- PrimeVue: Already configured in the app — DataTable, Dialog, Toast, Button etc. available

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 04-user-crud-views*
*Context gathered: 2026-03-11*
