# DEVHUB-008 — Create the React web application shell

|  |  |
|---|---|
| **Epic** | EPIC 1 — Foundation & project setup |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-001 |
| **Specs** | [`frontend-web-architecture.md`](../../tech-specs/frontend-web-architecture.md), [`screens-and-navigation.md`](../../screens-and-navigation.md) |

## Context

The shell is the frame every future screen slots into: providers, router, layout, API client and
the shared loading/empty/error components. Building it once prevents each feature inventing its
own.

## Scope

**In:** Vite + React + TS project, routing skeleton, providers, app shell layout, API client,
shared state components, Tailwind.
**Out:** real screens (their own epics), auth logic (DEVHUB-020).

## Tasks

- [ ] `npm create vite@latest web -- --template react-ts`; set `strict: true`.
- [ ] Install React Router, TanStack Query, Tailwind, React Hook Form, Zod.
- [ ] Create the folder structure from the spec (`app/`, `features/`, `shared/`).
- [ ] Build `AppShell` (sidebar + top bar + content outlet) and `AuthLayout`.
- [ ] Define the route tree with placeholder elements for the main routes.
- [ ] Implement `shared/api/client.ts` with `ApiError` and `ProblemDetails` parsing (refresh
      handling comes in DEVHUB-021).
- [ ] Implement `shared/components`: `Button`, `Skeleton`, `EmptyState`, `ErrorState`, `Modal`,
      `Drawer`, `Toast`.
- [ ] Add a route-level `ErrorBoundary` and a 404 screen.
- [ ] Configure `VITE_API_URL` and verify a call to `/health` renders in a debug page.

## Acceptance criteria

- [ ] `npm run dev` serves the shell at `http://localhost:5173`.
- [ ] Navigating between placeholder routes keeps the sidebar mounted.
- [ ] A deliberate error inside a route renders the error boundary, not a white screen.
- [ ] `npm run build` and `npm run lint` pass with no warnings.
- [ ] The shell renders correctly at 1280px and 768px.

## Technical notes

- Keep the shell dumb: no data fetching except the health probe. Feature routes own their data.
- Put the design tokens (colors for statuses and health) in one Tailwind config extension now —
  `screens-and-navigation.md` §8 lists them.

## Learning goals

Vite project structure, nested layout routes, provider composition, why a shared state-component
set beats per-feature spinners.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.
