# Frontend web architecture

React 18 + TypeScript + Vite. Feature-first, server-state-driven.

---

## 1. Stack decisions

| Concern | Choice | Why |
|---|---|---|
| Build | Vite | fast dev server, simple config, first-class TS |
| Language | TypeScript, `strict: true` | the API contract is the point; `any` defeats it |
| Routing | React Router v6 (data router) | nested layouts, URL-driven state |
| Server state | TanStack Query v5 | caching, invalidation, optimistic updates, retries |
| Client state | React state + Context; Zustand only if a real need appears | most "global state" here is server state |
| Forms | React Hook Form + Zod | typed schemas shared with API DTO shapes |
| Styling | Tailwind CSS + a few headless primitives (Radix) | speed, accessible primitives, no design-system project |
| HTTP | `fetch` wrapper (`apiClient`) | one place for auth, refresh, error normalization |
| Drag & drop | dnd-kit | accessible, maintained, works with keyboard |
| Tests | Vitest + Testing Library; Playwright for 2–3 smoke flows | |

Do not add: Redux, GraphQL, a component library that owns your design, SSR. None are needed.

---

## 2. Folder structure — feature first

```text
web/src/
├── main.tsx
├── App.tsx                     # router + providers
├── app/
│   ├── router.tsx
│   ├── providers.tsx           # QueryClient, Auth, Theme
│   └── layouts/                # AppShell, AuthLayout, ProjectLayout
├── features/
│   ├── auth/
│   │   ├── api/                # useLogin, useRegister, useMe
│   │   ├── components/
│   │   ├── hooks/
│   │   ├── pages/              # LoginPage, RegisterPage
│   │   └── types.ts
│   ├── workspaces/
│   ├── projects/
│   ├── issues/
│   ├── labels/
│   ├── comments/
│   ├── releases/
│   ├── environments/
│   ├── deployments/
│   ├── cicd/
│   ├── dashboard/
│   ├── search/
│   └── notifications/
├── shared/
│   ├── api/                    # apiClient, queryKeys, error types
│   ├── components/             # Button, Modal, Drawer, EmptyState, ErrorState, Skeleton
│   ├── hooks/                  # useDebounce, useQueryParams, useMediaQuery
│   ├── lib/                    # date, formatting, cn()
│   └── types/                  # shared DTO types
└── styles/
```

Rule: a feature may import from `shared/`, never from another feature's internals. If two
features need the same thing, it moves to `shared/`.

---

## 3. API client

One wrapper handles auth, refresh and error normalization:

```ts
// shared/api/client.ts
export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...authHeader(), ...init?.headers },
  });

  if (res.status === 401 && !isRefreshRequest(path)) {
    await refreshTokenOnce();          // single-flight; queues concurrent 401s
    return apiFetch<T>(path, init);    // retried exactly once
  }
  if (!res.ok) throw await toApiError(res);   // parses ProblemDetails
  return res.status === 204 ? (undefined as T) : res.json();
}
```

- `refreshTokenOnce()` is **single-flight**: concurrent 401s wait on one refresh promise.
- A failed refresh clears auth state and redirects to `/login?returnTo=…`.
- `ApiError` carries `status`, `title`, `detail`, `errors` (field → messages) so forms can map
  server validation onto fields.

---

## 4. Server state with TanStack Query

Centralized, typed query keys — never inline string arrays:

```ts
export const qk = {
  me: ['me'] as const,
  workspaces: ['workspaces'] as const,
  projects: (workspaceId: string) => ['workspaces', workspaceId, 'projects'] as const,
  issues: (projectId: string, filters: IssueFilters) =>
    ['projects', projectId, 'issues', filters] as const,
  issue: (issueId: string) => ['issues', issueId] as const,
  dashboard: (projectId: string) => ['projects', projectId, 'dashboard'] as const,
};
```

Defaults: `staleTime: 30_000`, `retry: 1`, `refetchOnWindowFocus: true` for dashboards and
environments (freshness matters), `false` for form-heavy screens.

**Optimistic update pattern** (board drag, status change):

```ts
onMutate: async (vars) => {
  await qc.cancelQueries({ queryKey: qk.issue(vars.id) });
  const previous = qc.getQueryData(qk.issue(vars.id));
  qc.setQueryData(qk.issue(vars.id), old => ({ ...old, status: vars.status }));
  return { previous };
},
onError: (_e, vars, ctx) => {
  qc.setQueryData(qk.issue(vars.id), ctx.previous);
  toast.error('Could not update the issue');
},
onSettled: (_d, _e, vars) => {
  qc.invalidateQueries({ queryKey: qk.issue(vars.id) });
  qc.invalidateQueries({ queryKey: ['projects', vars.projectId, 'issues'] });
},
```

---

## 5. URL as state

Filters, sorting, pagination and the open issue live in the URL:

```text
/p/DEV/issues?status=InProgress&priority=High&assignee=me&label=backend&sort=-updatedAt&page=2
```

A `useIssueFilters()` hook parses and serializes with Zod, so a shared link reproduces the exact
view and browser back/forward works. Component state is only for things nobody would share
(open dropdown, hover).

---

## 6. Required UI states

Every data view implements four states, using shared components:

```text
<Skeleton/>      loading (skeleton matching the final layout, not a spinner)
<EmptyState/>    no data + the action that fixes it
<ErrorState/>    message + Retry button
                 success
```

An `ErrorBoundary` wraps each route element so a render crash degrades one screen, not the app.

---

## 7. Auth in the client

- Access token in memory (module-scoped variable), refresh token in an `httpOnly` cookie if the
  API can set one, otherwise `localStorage` with the trade-off documented in DEVHUB-020.
- `AuthProvider` bootstraps by calling `GET /api/me`; while pending it renders a splash, so
  protected routes never flash the login screen.
- `<RequireAuth>` wraps the private route tree; `<RequireWorkspaceRole role="Owner">` guards
  settings routes. Client-side guards are UX only — the API is the real authority.

---

## 8. Performance

- Route-level code splitting with `React.lazy` for board, dashboard and settings.
- Virtualize the issue list beyond ~200 rows (`@tanstack/react-virtual`).
- Memoize board columns; drag must not re-render the whole board.
- Debounce search input 250 ms and abort in-flight requests on change.
- Target: dashboard interactive in < 1.5 s on a warm cache.

---

## 9. Accessibility baseline

- Semantic elements; a button is a `<button>`.
- Modals and drawers trap focus, close on Esc, restore focus on close.
- Board drag & drop has a keyboard alternative (status dropdown in the card menu).
- Visible focus rings; color never the only signal; labels tied to inputs.
- Live region announces toasts.

---

## 10. Environment configuration

```text
VITE_API_URL=http://localhost:5080
VITE_APP_ENV=local|staging|production
```

Only `VITE_`-prefixed variables reach the bundle. **Never put a secret in a Vite variable** —
the bundle is public. Build once per environment; the SPA is deployed to S3 + CloudFront
(DEVHUB-098).
