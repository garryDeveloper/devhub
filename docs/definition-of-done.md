# Definition of Done

A ticket is not done when the code compiles. It is done when every applicable box below is
checked. Every ticket references this file instead of repeating it.

---

## 1. Every ticket

- [ ] All acceptance criteria in the ticket are demonstrably met.
- [ ] The change is scoped to the ticket — no unrelated refactors smuggled in.
- [ ] No commented-out code, no `TODO` without a follow-up ticket id.
- [ ] No secrets, connection strings or personal data committed.
- [ ] CI is green.
- [ ] Documentation in `docs/` updated if a contract, schema or flow changed.
- [ ] You can explain the change out loud: what it does, why it is designed that way, and what
      would break it. (This project is a learning project — this box is not optional.)

## 2. Backend tickets

- [ ] Domain rules live in `DevHub.Domain`; no EF Core / ASP.NET / AWS types leak into it.
- [ ] Request validation implemented (FluentValidation or explicit) with per-field errors.
- [ ] Authorization verified: the caller's workspace/project membership is checked in the
      application layer, not only in the controller.
- [ ] Resources the caller may not see return `404`, not `403`.
- [ ] Error cases return RFC 7807 `ProblemDetails` with the right status code.
- [ ] Unit tests cover the business rules and at least one failure path.
- [ ] Integration test covers the happy path and one auth/validation failure.
- [ ] EF Core migration exists and applies cleanly on an empty database.
- [ ] Queries touched are index-supported; no N+1 introduced (check the generated SQL once).
- [ ] Swagger/OpenAPI reflects the endpoint, including response types and status codes.
- [ ] Logs are meaningful and structured — no `Console.WriteLine`, no logging of secrets or
      full request bodies.

## 3. Web tickets

- [ ] Loading, empty and error states implemented for every data view.
- [ ] Server state goes through TanStack Query with correct cache keys and invalidation.
- [ ] Optimistic updates roll back correctly on failure and surface a message.
- [ ] Forms show inline validation and disable submit while pending.
- [ ] Filters and view state that should be shareable live in the URL.
- [ ] Keyboard accessible: focus states visible, modals trap focus and close on Esc.
- [ ] Meaning is never conveyed by color alone.
- [ ] Works at 1280px and at 768px width.
- [ ] TypeScript strict, no `any`, no `@ts-ignore`.
- [ ] `npm run lint` and `npm run build` pass.

## 4. Mobile tickets

- [ ] Screen works on iOS and Android (simulator is acceptable).
- [ ] Loading, empty and error states implemented; pull-to-refresh where a list exists.
- [ ] Tokens stored in secure storage (Keychain / Keystore), never `AsyncStorage`.
- [ ] Navigation params typed; deep link handled if the screen is a notification target.
- [ ] Touch targets ≥ 44pt; safe-area insets respected.
- [ ] No console errors or yellow-box warnings introduced.

## 5. Infrastructure / AWS tickets

- [ ] The AWS resource is created and reachable as described.
- [ ] IAM permissions are least-privilege — no `*` on actions or resources without a written
      justification in the ticket.
- [ ] No long-lived credentials created; CI uses OIDC + assumed role.
- [ ] Configuration and endpoints documented in `docs/tech-specs/aws-architecture.md`.
- [ ] The change is reproducible: exact console steps or CLI commands recorded in the ticket
      or in `infrastructure/`.
- [ ] Cost impact noted (instance class, storage, retention) and kept in the free/low tier
      where possible.
- [ ] Teardown steps documented — you must be able to delete it cleanly.

## 6. CI/CD tickets

- [ ] The workflow runs on the intended triggers and no others.
- [ ] Jobs fail loudly: a failing test or lint must fail the workflow.
- [ ] Secrets come from GitHub secrets/environments, never from the workflow file.
- [ ] Caching configured for restore/install steps.
- [ ] Production deployment requires manual approval.
- [ ] The workflow's outcome is visible in DevHub itself once DEVHUB-107 is done.

---

## 7. Pull request checklist

```text
Ticket:        DEVHUB-XXX
What changed:  one paragraph
Why:           the decision and the alternative you rejected
How to verify: exact steps a reviewer follows
Docs updated:  yes / not needed (why)
Screenshots:   for any UI change
```

Small PRs, one ticket each. A PR that touches backend, web and mobile at once is a sign the
ticket was too big — split it.
