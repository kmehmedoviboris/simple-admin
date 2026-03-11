# Phase 4: User CRUD Views - Research

**Researched:** 2026-03-11
**Domain:** Vue 3 SPA with PrimeVue 4 — DataTable, ConfirmDialog, Toast, form validation
**Confidence:** HIGH

## Summary

Phase 4 builds on a fully-working Vue 3 + PrimeVue 4 + Pinia SPA shell (Phase 3). The stack is already installed and configured: PrimeVue 4.5.4 with Aura theme, primeicons 7, hey-api client-fetch 0.13, vue-router 5, and pinia 3. No new packages are required except registering two PrimeVue service plugins (ToastService, ConfirmationService) in `main.ts` — which is the single most critical setup step.

The generated API client (`sdk.gen.ts`) exposes all five operations needed: `getApiUsers`, `postApiUsers`, `putApiUsersById`, `deleteApiUsersById`, and `getApiMe`. The hey-api client returns `{ data, error, response }` — callers check `if (error)` to detect failures; `error` carries the typed `ProblemDetails` shape for 4xx/5xx responses. The 401 interceptor in `main.ts` already handles token expiry globally, so views only need to handle non-401 errors.

The `/api/me` endpoint returns `{ sub: string, email: string }` (typed as `unknown` in the generated client) and is the source of truth for identifying the currently logged-in user to prevent self-delete. The `GetApiMeResponses` type is `unknown`, so the response body must be cast: `const me = data as { sub: string; email: string }`.

**Primary recommendation:** Register ToastService and ConfirmationService in `main.ts`, place `<Toast position="top-right" />` and `<ConfirmDialog />` in `App.vue`, then build the three views using `useToast` and `useConfirm` composables.

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Form navigation**
- Separate pages for create and edit: `/users/new` and `/users/:id/edit`
- Each form is a full page with its own route (not modals or drawers)
- After successful save (create or edit), navigate back to `/users` list
- Shared `UserForm.vue` component used by both `UserCreateView.vue` and `UserEditView.vue`
- Create passes empty data; edit pre-fills from existing user
- Password field is required on create, optional on edit
- Page heading: "Create User" or "Edit User" at the top of each form page

**Table layout**
- DataTable columns: Email + Actions (no emailConfirmed column)
- Actions column: inline Edit and Delete icon buttons on each row
- "Create User" button above the table, right-aligned
- Loading spinner visible while the user list API request is in flight
- Prevent self-delete: disable delete button on the row matching the currently logged-in user (use `/api/me` to identify)

**Validation rules**
- Validation triggers on submit only (not on blur or keystroke)
- Email field: required + basic email format validation (contains @ and domain)
- Password field on create: required only (no length or complexity rules client-side)
- Password field on edit: optional (leave blank to keep current password)
- Inline error messages displayed under each invalid field
- Server-side validation errors shown as toast only — no inline mapping of server errors

**Error & success feedback**
- PrimeVue Toast for both success and error messages
- Success toasts: green (`severity="success"`), for create ("User created"), edit ("User updated"), delete ("User deleted")
- Error toasts: red (`severity="error"`), for API failures (4xx/5xx)
- Toast position: top-right
- Auto-dismiss after 3 seconds (`life: 3000`) for all toasts
- 401 errors still trigger redirect to login (handled by existing `main.ts` interceptor — no toast for 401)

### Claude's Discretion
- PrimeVue component variants and styling choices
- Loading indicator implementation (spinner vs skeleton vs overlay)
- Exact icon choices for edit/delete buttons
- Form field layout (stacked, grid, card wrapper)
- How to identify current user for self-delete prevention (token claims vs /api/me call)

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| UX-01 | Forms display inline validation errors for required fields and email format | Manual validation with `ref()` error map, `invalid` prop on InputText/Password, small Message component under each field |
| UX-02 | List view and form submissions show loading/busy indicators | DataTable `:loading` prop with `loadingIcon`, Button `:loading` prop on submit |
| UX-03 | API failures (4xx/5xx) display toast error messages to the user | `useToast` composable + `toast.add({ severity: 'error', ... })` in catch blocks; `error.detail` from ProblemDetails |
</phase_requirements>

---

## Standard Stack

### Core (already installed)
| Library | Version | Purpose | Note |
|---------|---------|---------|------|
| primevue | 4.5.4 | UI components: DataTable, Button, InputText, Password, Toast, ConfirmDialog, Message | Already installed |
| primeicons | 7.0.0 | Icon font (`pi pi-pencil`, `pi pi-trash`, etc.) | Already installed |
| @primeuix/themes | 2.0.3 | Aura theme preset | Already installed |
| pinia | 3.0.4 | State management | Already installed |
| vue-router | 5.0.3 | SPA routing | Already installed |
| @hey-api/client-fetch | 0.13.1 | Typed API client | Already installed |

### Services to Register (missing from main.ts)
| Service | Import | Purpose |
|---------|--------|---------|
| ToastService | `import ToastService from 'primevue/toastservice'` | Enables `useToast()` composable across the app |
| ConfirmationService | `import ConfirmationService from 'primevue/confirmationservice'` | Enables `useConfirm()` composable across the app |

**No new npm packages needed.** All required PrimeVue components ship with the existing `primevue` 4.5.4 install.

### Installation (service registration only)
```typescript
// main.ts — add these two lines after app.use(PrimeVue, ...)
import ToastService from 'primevue/toastservice'
import ConfirmationService from 'primevue/confirmationservice'

app.use(ToastService)
app.use(ConfirmationService)
```

## Architecture Patterns

### Recommended Project Structure
```
src/
├── views/
│   ├── UsersView.vue          # Replace placeholder — DataTable list with loading + delete confirm
│   ├── UserCreateView.vue     # New — wraps UserForm with empty initial data
│   └── UserEditView.vue       # New — fetches user by :id, wraps UserForm with pre-filled data
├── components/
│   ├── AppShell.vue           # Existing — unchanged
│   └── UserForm.vue           # New — shared form with email + password fields + inline errors
└── router/
    └── index.ts               # Add routes: /users/new and /users/:id/edit
```

### Pattern 1: DataTable with Loading State
**What:** Bind `:loading` to a `ref<boolean>` and set it around the `getApiUsers()` call. The `loadingIcon` prop shows a spinner overlay on the table body.
**When to use:** Any time a list fetch is in flight.
```vue
<script setup lang="ts">
import { ref, onMounted } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import { getApiUsers } from '@/client/sdk.gen'
import type { UserListDto } from '@/client/types.gen'

const users = ref<UserListDto[]>([])
const loading = ref(false)

async function fetchUsers() {
  loading.value = true
  const { data, error } = await getApiUsers()
  loading.value = false
  if (error) {
    // handle error (toast)
    return
  }
  users.value = data ?? []
}

onMounted(fetchUsers)
</script>

<template>
  <DataTable :value="users" :loading="loading" loadingIcon="pi pi-spinner pi-spin">
    <Column field="email" header="Email" />
    <Column header="Actions">
      <template #body="{ data: row }">
        <!-- edit + delete buttons -->
      </template>
    </Column>
  </DataTable>
</template>
```

### Pattern 2: ConfirmDialog for Delete
**What:** Use `useConfirm()` composable to trigger a confirmation dialog before a destructive action. `ConfirmDialog` must be in the template (placed in `App.vue` so it's always mounted).
**When to use:** Delete button click on a DataTable row.
```vue
<script setup lang="ts">
import { useConfirm } from 'primevue/useconfirm'
import ConfirmDialog from 'primevue/confirmdialog'

const confirm = useConfirm()

function deleteUser(id: string) {
  confirm.require({
    message: 'Are you sure you want to delete this user?',
    header: 'Delete User',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Cancel',
    acceptLabel: 'Delete',
    acceptClass: 'p-button-danger',
    accept: async () => {
      const { error } = await deleteApiUsersById({ path: { id } })
      if (error) {
        toast.add({ severity: 'error', summary: 'Error', detail: (error as any).detail ?? 'Delete failed', life: 3000 })
        return
      }
      toast.add({ severity: 'success', summary: 'Deleted', detail: 'User deleted', life: 3000 })
      await fetchUsers()
    }
  })
}
</script>
```

### Pattern 3: Toast Notifications
**What:** `useToast()` composable provides `toast.add()`. The `<Toast />` component must be mounted globally in `App.vue`.
**When to use:** After any API call completes (success or error).
```vue
<!-- App.vue — add these two global overlay components -->
<template>
  <Toast position="top-right" />
  <ConfirmDialog />
  <AppShell v-if="showShell">
    <RouterView />
  </AppShell>
  <RouterView v-else />
</template>
```

```typescript
// In any view/component
import { useToast } from 'primevue/usetoast'
const toast = useToast()

// Success
toast.add({ severity: 'success', summary: 'Created', detail: 'User created', life: 3000 })

// Error — extract message from ProblemDetails
toast.add({ severity: 'error', summary: 'Error', detail: (error as any).detail ?? 'An error occurred', life: 3000 })
```

### Pattern 4: Submit-only Form Validation (manual, no library)
**What:** Use a plain `ref<Record<string, string>>` for error messages. On submit, validate fields and populate errors; if any errors exist, abort API call. Mark inputs `invalid` using the `:invalid` prop.
**When to use:** Simple forms where Vuelidate/VeeValidate overhead is not justified (POC).
```vue
<script setup lang="ts">
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Message from 'primevue/message'

const errors = ref<Record<string, string>>({})

function validate(): boolean {
  errors.value = {}
  if (!email.value) {
    errors.value.email = 'Email is required'
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value)) {
    errors.value.email = 'Enter a valid email address'
  }
  if (isCreate && !password.value) {
    errors.value.password = 'Password is required'
  }
  return Object.keys(errors.value).length === 0
}

async function onSubmit() {
  if (!validate()) return
  submitting.value = true
  // ... API call
  submitting.value = false
}
</script>

<template>
  <div class="field">
    <label for="email">Email</label>
    <InputText id="email" v-model="email" :invalid="!!errors.email" fluid />
    <Message v-if="errors.email" severity="error" size="small" variant="simple">{{ errors.email }}</Message>
  </div>
  <div class="field">
    <label for="password">Password</label>
    <Password id="password" v-model="password" :invalid="!!errors.password" :feedback="false" fluid toggleMask />
    <Message v-if="errors.password" severity="error" size="small" variant="simple">{{ errors.password }}</Message>
  </div>
  <Button label="Save" type="submit" :loading="submitting" @click="onSubmit" />
</template>
```

### Pattern 5: Self-Delete Prevention via /api/me
**What:** Fetch the current user's email once on mount of `UsersView.vue`. Compare against each row's `email` to conditionally disable the delete button.
**When to use:** Rendering the DataTable actions column.

The `/api/me` endpoint returns `{ sub: string, email: string }` (OpenAPI typed as `unknown`). Cast the result:
```typescript
const { data } = await getApiMe()
const currentUserEmail = (data as { sub?: string; email?: string })?.email ?? null
```
Then in the template: `:disabled="row.email === currentUserEmail"`.

**Note:** Using email (not sub/ID) comparison is safe here because the token always includes the correct email claim. Alternatively, compare by `sub` (the user ID in the token) if the backend returns the user's GUID as `sub` — check `/api/me` response to confirm. The `UserListDto.id` (GUID) should equal the `sub` claim if OpenIddict was configured to include `Subject` destination (it was, per Phase 1 decisions).

### Pattern 6: hey-api Error Handling
**What:** All SDK functions return `{ data, error, response }`. Check `error` for API failures. The `error` value is typed as `ProblemDetails` for 4xx responses per the generated types.
```typescript
const { data, error } = await postApiUsers({ body: { email: email.value, password: password.value } })
if (error) {
  // error is typed as ProblemDetails | unknown depending on status code
  const detail = (error as { detail?: string })?.detail ?? 'An error occurred'
  toast.add({ severity: 'error', summary: 'Error', detail, life: 3000 })
  return
}
// data is UserListDto on success
```

### Anti-Patterns to Avoid
- **Calling `useToast()` or `useConfirm()` before services are registered:** `app.use(ToastService)` and `app.use(ConfirmationService)` MUST appear in `main.ts` before the composables are used. Missing this causes a silent failure or runtime error.
- **Placing `<Toast />` or `<ConfirmDialog />` inside a view component:** They must be in `App.vue` (root template) so they are always mounted regardless of which route is active.
- **Triggering validation on blur/keystroke:** Locked decision — validation triggers on submit only.
- **Mapping server errors to inline fields:** Server errors (duplicate email, etc.) go to toast only — no inline field mapping.
- **Using `ThrowOnError: true` generic on SDK calls:** The existing interceptor pattern uses the default `ThrowOnError = false`, which returns `{ data, error }` safely. Don't change this globally.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Delete confirmation | Custom modal/dialog state | PrimeVue ConfirmDialog + useConfirm() | Already installed, handles keyboard, accessibility, backdrop |
| Toast notifications | Custom toast component | PrimeVue Toast + useToast() | Already installed, handles stacking, auto-dismiss, severity styles |
| Password toggle visibility | Manual input type swap | PrimeVue Password with `toggleMask` prop | Built in — single prop |
| Loading spinner on table | Custom overlay div | DataTable `:loading` prop | Single prop, handles overlay positioning |
| Loading spinner on button | Disabled + spinner span | Button `:loading` prop | Single prop, handles icon + disabled state |

**Key insight:** All the UX patterns required for this phase (loading states, confirmation dialogs, toasts, password input) are built into the existing PrimeVue 4.5.4 install. The work is wiring, not building.

## Common Pitfalls

### Pitfall 1: Missing Service Registrations
**What goes wrong:** `useToast()` or `useConfirm()` throws "No PrimeVue Toast/Confirm provided!" at runtime. No TypeScript error — fails silently or at call time.
**Why it happens:** `ToastService` and `ConfirmationService` are opt-in plugins not included in the base `app.use(PrimeVue, ...)` call.
**How to avoid:** Add `app.use(ToastService)` and `app.use(ConfirmationService)` in `main.ts` immediately after `app.use(PrimeVue, ...)`.
**Warning signs:** Console error "No PrimeVue Toast/ConfirmationService provided" when calling `toast.add()` or `confirm.require()`.

### Pitfall 2: Toast / ConfirmDialog Not Mounted
**What goes wrong:** Toast or dialog trigger call succeeds but nothing renders — the overlay component is not in the DOM.
**Why it happens:** `<Toast />` and `<ConfirmDialog />` must be mounted somewhere in the component tree when the composable fires. If placed only inside a specific view, they unmount when that view unmounts.
**How to avoid:** Place both in `App.vue` template, outside `<RouterView>`.
**Warning signs:** Toast fires (no error) but nothing visible; ConfirmDialog fires but no dialog appears.

### Pitfall 3: Router Routes Added in Wrong Order
**What goes wrong:** `/users/new` matches the `:id/edit` route or is shadowed by the `/users` route.
**Why it happens:** vue-router 5 uses first-match routing; `/users/new` must be defined before `/users/:id/edit` in the routes array, and after the `/users` catch-all.
**How to avoid:** Order routes: `/users` → `/users/new` → `/users/:id/edit`.
**Warning signs:** Navigating to `/users/new` renders the edit view with `id="new"`.

### Pitfall 4: GetApiMe Response Typed as `unknown`
**What goes wrong:** TypeScript error when accessing `.email` on the `data` from `getApiMe()`.
**Why it happens:** The OpenAPI spec for `/api/me` uses `200: unknown` in the generated types because the backend returns an anonymous object without a named DTO.
**How to avoid:** Cast: `const me = data as { sub?: string; email?: string }`. Do not modify the generated types file.
**Warning signs:** TypeScript error "Property 'email' does not exist on type 'unknown'".

### Pitfall 5: Self-Delete Check Using Wrong Field
**What goes wrong:** Self-delete check fails to prevent deletion, or disables wrong rows.
**Why it happens:** Comparing `row.id` (GUID from UserListDto) against `sub` from `/api/me` works only if OpenIddict puts the user GUID in the `sub` claim — which it does (per Phase 1: `OpenIddictConstants.Claims.Subject` destination set). However, the `/api/me` response is typed `unknown` so the cast is required.
**How to avoid:** Fetch `/api/me` on mount of UsersView, cast result, store `currentUserEmail` as a `ref<string | null>`. Compare `row.email === currentUserEmail` in the template `:disabled` binding.
**Warning signs:** All delete buttons disabled, or none disabled.

### Pitfall 6: Password Component in Edit Form Still Shows Strength Meter
**What goes wrong:** Clicking the password field in edit form shows a strength indicator popup, confusing users since the field is optional.
**Why it happens:** `feedback` prop defaults to `true` on PrimeVue Password.
**How to avoid:** Always pass `:feedback="false"` on the Password component in this project (no password policy UI needed for POC).
**Warning signs:** Strength indicator popup appears on focus.

### Pitfall 7: Button Loading State Conflicts with Disabled
**What goes wrong:** A button is simultaneously `:loading="true"` and `:disabled="true"`, creating unclear visual state (known PrimeVue v4 issue #7713).
**Why it happens:** PrimeVue 4 Button's `loading` prop sets `disabled` internally; passing `disabled` separately can conflict.
**How to avoid:** Use `:loading` alone for submit buttons — don't also bind `:disabled` during submission. Only use `:disabled` for the self-delete prevention case (separate button, not the submit button).
**Warning signs:** Submit button appears disabled before the API call starts.

## Code Examples

Verified patterns for this phase:

### Router Registration for New Routes
```typescript
// src/router/index.ts — insert after /users route
{
  path: '/users/new',
  component: () => import('@/views/UserCreateView.vue'),
},
{
  path: '/users/:id/edit',
  component: () => import('@/views/UserEditView.vue'),
},
```

### main.ts Service Registration
```typescript
import ToastService from 'primevue/toastservice'
import ConfirmationService from 'primevue/confirmationservice'

app.use(pinia)
app.use(router)
app.use(PrimeVue, { theme: { preset: Aura } })
app.use(ToastService)        // ADD
app.use(ConfirmationService) // ADD
```

### App.vue Global Overlay Components
```vue
<template>
  <Toast position="top-right" />
  <ConfirmDialog />
  <AppShell v-if="showShell">
    <RouterView />
  </AppShell>
  <RouterView v-else />
</template>

<script setup lang="ts">
import Toast from 'primevue/toast'
import ConfirmDialog from 'primevue/confirmdialog'
// ... existing imports
</script>
```

### UserForm.vue Props Interface
```typescript
interface Props {
  modelValue: { email: string; password: string }
  isCreate: boolean          // true = create (password required), false = edit (password optional)
  loading: boolean           // controls submit button :loading
}

const emit = defineEmits<{
  'update:modelValue': [value: { email: string; password: string }]
  'submit': [value: { email: string; password: string }]
}>()
```

### Delete with Confirmation
```typescript
const confirm = useConfirm()
const toast = useToast()

function confirmDelete(user: UserListDto) {
  confirm.require({
    message: `Delete ${user.email}?`,
    header: 'Confirm Delete',
    icon: 'pi pi-exclamation-triangle',
    acceptClass: 'p-button-danger',
    accept: async () => {
      const { error } = await deleteApiUsersById({ path: { id: user.id } })
      if (error) {
        toast.add({ severity: 'error', summary: 'Error', detail: (error as any)?.detail ?? 'Delete failed', life: 3000 })
        return
      }
      toast.add({ severity: 'success', summary: 'Deleted', detail: 'User deleted', life: 3000 })
      users.value = users.value.filter(u => u.id !== user.id)
    }
  })
}
```

### Email Regex for Submit Validation
```typescript
// Basic email format: contains @ with something on both sides and a dot in the domain
const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
if (!EMAIL_RE.test(email.value)) {
  errors.value.email = 'Enter a valid email address'
}
```

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| PrimeVue 3 Options API | PrimeVue 4 Composition API with `<script setup>` | Composables (`useToast`, `useConfirm`) replace `this.$toast`/`this.$confirm` |
| PrimeVue 3 `invalid` via CSS class | PrimeVue 4 `:invalid` prop on input components | Native invalid state, no custom CSS needed |
| Separate `primevue/components/...` deep imports | Direct `primevue/[component]` imports | Project already uses the current pattern |
| `vue-router 4` dynamic params | `vue-router 5` (installed) | Same API, `useRoute().params.id` unchanged |

**Deprecated/outdated:**
- `this.$toast` and `this.$confirm` — Options API only; use `useToast()`/`useConfirm()` composables with `<script setup>`.

## Open Questions

1. **Self-delete by sub vs email**
   - What we know: `/api/me` returns `{ sub, email }`. `UserListDto` has `id` and `email`. OpenIddict was configured in Phase 1 to set `Subject` destination on the sub claim.
   - What's unclear: Whether `sub` in the token equals `UserListDto.id` exactly (GUID format). If so, comparing `row.id === me.sub` is more robust than email comparison (emails can change).
   - Recommendation: Use email comparison (`row.email === me.email`) as the safe fallback since the `/api/me` response is untyped. Both fields exist. If in doubt, fetch `/api/me` and compare both — first match wins.

2. **`getApiMe` return type is `unknown`**
   - What we know: The generated type is `200: unknown` because the backend returns an anonymous object `new { sub, email }`.
   - What's unclear: No TypeScript safety on the cast.
   - Recommendation: Cast with `as { sub?: string; email?: string }` — acceptable for POC. If the backend is updated to return a named DTO in a future phase, the types will be regenerated.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Playwright 1.58.2 |
| Config file | `simple-admin-spa/playwright.config.ts` |
| Quick run command | `cd simple-admin-spa && npx playwright test tests/auth.spec.ts` |
| Full suite command | `cd simple-admin-spa && npx playwright test` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| UX-01 | Forms show inline validation errors on submit with invalid input | e2e | `npx playwright test tests/crud.spec.ts --grep "validation"` | ❌ Wave 0 |
| UX-02 | DataTable shows loading indicator while API is in flight; submit button shows loading state | e2e | `npx playwright test tests/crud.spec.ts --grep "loading"` | ❌ Wave 0 |
| UX-03 | API failures (4xx/5xx) display toast error messages | e2e | `npx playwright test tests/crud.spec.ts --grep "error toast"` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `cd simple-admin-spa && npx playwright test tests/auth.spec.ts` (existing auth smoke — ensures nothing regressed)
- **Per wave merge:** `cd simple-admin-spa && npx playwright test`
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `simple-admin-spa/tests/crud.spec.ts` — covers UX-01, UX-02, UX-03 (list view, create, edit, delete, validation, toasts)
- [ ] Shared `loginAsAdmin` helper already exists in `tests/auth.spec.ts` — extract to a `tests/helpers.ts` module or copy inline

## Sources

### Primary (HIGH confidence)
- Official PrimeVue docs — https://primevue.org/datatable/ (DataTable loading, column templates)
- Official PrimeVue docs — https://primevue.org/toast/ (ToastService, useToast, placement)
- Official PrimeVue docs — https://primevue.org/confirmdialog/ (ConfirmationService, useConfirm, require())
- Official PrimeVue docs — https://primevue.org/password/ (feedback prop, toggleMask)
- Official PrimeVue docs — https://primevue.org/button/ (loading prop behavior)
- Project source: `src/client/types.gen.ts` — verified DTO shapes and error types
- Project source: `src/client/sdk.gen.ts` — verified SDK function signatures
- Project source: `SimpleAdmin.Api/Controllers/ApiController.cs` — verified /api/me response shape
- hey-api GitHub issue #691 — verified `{ data, error, response }` destructure pattern

### Secondary (MEDIUM confidence)
- WebSearch: PrimeVue 4 ConfirmDialog script setup pattern (consistent across multiple sources)
- WebSearch: ToastService and ConfirmationService registration requirement (consistent with official docs structure)
- WebSearch: DataTable `:loading` + `loadingIcon` prop (consistent with official docs)
- WebSearch: Password `:feedback="false"` for disabling strength meter

### Tertiary (LOW confidence)
- PrimeVue GitHub issue #7713 — Button loading+disabled conflict (may be fixed in 4.5.4; treat as caution, not blocker)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all packages confirmed installed via package.json
- Architecture: HIGH — verified against actual source files and generated types
- Pitfalls: HIGH (service registration, component placement) / MEDIUM (button loading conflict — from GitHub issue, may not affect 4.5.4)
- Test gaps: HIGH — confirmed no crud.spec.ts exists

**Research date:** 2026-03-11
**Valid until:** 2026-06-11 (PrimeVue 4.x stable; patterns unlikely to change in 90 days)
