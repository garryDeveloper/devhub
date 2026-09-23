# Authentication & authorization

Application-level auth in the API. AWS IAM protects infrastructure, never application users —
the two are unrelated and must not be confused.

---

## 1. Model

```text
Access token   JWT, 15 minutes, stateless, sent as: Authorization: Bearer <token>
Refresh token  opaque random 256-bit, 30 days, stored hashed, rotated on every use
```

Why both: a short-lived stateless JWT keeps every request cheap (no DB hit to authenticate),
while a stored refresh token gives real revocation. A long-lived JWT would be fast but
impossible to revoke; a session lookup on every request would be revocable but slow.

---

## 2. Password handling

- ASP.NET Core `PasswordHasher<User>` (PBKDF2-HMAC-SHA512, 100k+ iterations, per-user salt).
  Argon2id via `Konscious.Security.Cryptography` is an acceptable upgrade — decide in DEVHUB-013.
- Minimum 10 characters. Check against a small common-password list. No composition rules
  (forced symbols make passwords worse, not better).
- Never log, return or store the plaintext. `PasswordHash` never appears in a DTO.
- On login failure return a single generic message — never reveal whether the email exists.
- Constant-time comparison and a dummy hash verification on unknown emails so response timing
  does not leak account existence.

---

## 3. Access token (JWT)

```jsonc
{
  "sub": "9f1c…",                 // user id
  "email": "dario@example.com",
  "name": "Dario",
  "jti": "…",                     // unique per token
  "iat": 1757160000,
  "exp": 1757160900,              // +15 min
  "iss": "devhub-api",
  "aud": "devhub-clients"
}
```

- Algorithm HS256 with a 256-bit secret in the MVP (single service). Move to RS256 if a second
  service ever needs to validate tokens independently.
- **Workspace roles are not in the token.** Membership changes must take effect immediately, so
  roles are read from the database per request. The token only proves identity.
- Validate issuer, audience, lifetime and signature. `ClockSkew = TimeSpan.Zero` — otherwise the
  default 5-minute skew makes a 15-minute token last 20.
- Secret comes from configuration (user-secrets locally, SSM Parameter Store in AWS), never
  from `appsettings.json`.

---

## 4. Refresh token rotation

```text
POST /api/auth/refresh { refreshToken }
   ↓ hash it (SHA-256) and look up the row by token_hash
   ↓ not found                → 401
   ↓ revoked_at IS NOT NULL   → reuse: revoke the family, log a security warning, 401
   ↓ expires_at <= now        → 401
   ↓ else → issue a new access token AND a new refresh token (same family_id)
            mark the old row revoked, set replaced_by_token_id — one transaction
```

Every failure is the same `401 auth.invalid_refresh_token`; the client's only correct reaction to
any of them is to send the user to login.

**Families.** Register and login start a new *family* (`family_id` = the first token's id); every
rotation's successor inherits it. A family is one session, with at most one active token.

**Reuse detection:** if a refresh token that is already revoked is presented, every still-active
token of its family is revoked in one `UPDATE … WHERE family_id = @f` and the event is logged as
a security warning (user, family and token ids — never the token). That turns a stolen token into
a forced logout instead of a silent parallel session. Other sessions of the same user are not
touched.

- DECISION (DEVHUB-016): revoke by `family_id` instead of walking `replaced_by_token_id` link by
  link. A 30-day session refreshed every 15 minutes is a chain of ~2,900 rows; the family makes it
  one indexed statement. `replaced_by_token_id` stays as the audit trail.
- DECISION (DEVHUB-016): **optimistic concurrency** on PostgreSQL's `xmin`. Two parallel refreshes
  with the same token both read it as active; the second UPDATE finds `xmin` changed and fails,
  so exactly one successor is ever issued and the loser gets `401`. If the loser reads *after* the
  winner commits, it sees a revoked token and triggers reuse detection, which also ends the
  winner's session — hence the single-flight client rule below.
- No separate fixed-time comparison: the lookup is by the SHA-256 of the presented token, whose
  bytes an attacker cannot choose, so the index lookup's timing reveals nothing about stored
  tokens.
- Cleanup: `RefreshTokenCleanupService` (in-process `BackgroundService`) deletes rows whose
  `expires_at` is more than 60 days past, one minute after startup and then daily. Revoked rows
  need no separate rule — every row expires.

Client rules:

- The refresh call is **single-flight**: concurrent 401s wait on one in-flight refresh, otherwise
  parallel refreshes rotate each other and log the user out.
- A request is retried at most once after a refresh.
- Storage: web keeps the access token in memory and the refresh token in an `httpOnly; Secure;
  SameSite=Strict` cookie if the API sets one, otherwise `localStorage` with the XSS trade-off
  documented in DEVHUB-020. Mobile always uses `expo-secure-store`.

### Logout (DEVHUB-017)

```text
POST /api/auth/logout { refreshToken }          anonymous · no rate limit · always 204
   ↓ missing/empty token      → 204
   ↓ hash, look up by token_hash
   ↓ not found                → 204
   ↓ found (any state)        → revoke every active token of its family → 204
```

- **Always `204`.** Logout is idempotent (a second call finds the family already dead), and
  answering differently for an unknown, expired or revoked token would make the endpoint an
  oracle for token validity. Garbage, an empty string or an empty body are all `204`, not `400`.
- **Revokes the family, not just the token.** A stale, already-rotated token (e.g. another tab)
  still ends the session. It uses the same `UPDATE … WHERE family_id = @f` as reuse detection.
  It is not logged as reuse: presenting a token in order to *end* a session is not an attack.
  Other sessions of the same user are not touched; "log out all devices" is post-MVP (§8).
- DECISION (DEVHUB-017): **anonymous**, like `/refresh`. The refresh token is the credential.
  Requiring a Bearer token would force a client whose access token had just expired to rotate
  its refresh token only to revoke it. Outside the register+login rate-limit bucket for the
  same reasons as `/refresh` (§6).

**Residual access-token window.** Logout cannot revoke an access token that was already issued.
It stays valid until its `exp`, **at most 15 minutes** after logout. This is the accepted cost of
stateless JWTs, and it is pinned by the test
`LogoutTests.An_access_token_issued_before_logout_stays_valid_until_it_expires`.

- The alternative is an access-token **denylist**: store the `jti` of logged-out tokens until
  their `exp`, and check it on every request. That closes the window, but it adds a store lookup
  to *every* authenticated request, which is exactly what a stateless JWT exists to avoid. It also
  adds a store that every API instance must share. Not worth it for the MVP. The honest
  mitigations are the short lifetime and the client clearing the token from memory.

**Client contract** (implemented in DEVHUB-020 web / DEVHUB-022 mobile):

1. Read the stored refresh token, then call `POST /api/auth/logout` with it. Do not block the UI
   on the result, and ignore failures: there is nothing the user can do about a failed logout,
   and the local cleanup below is what actually logs them out on this device.
2. Clear the in-memory access token.
3. Clear the stored refresh token (web: `localStorage` / the cookie; mobile: `expo-secure-store`).
4. Clear the TanStack Query cache (`queryClient.clear()`), so the next user never sees the
   previous user's data.
5. Redirect to login (web `/login`; mobile: reset navigation to `AuthStack`) with history
   replaced, so going back cannot re-enter protected screens.

Step 1 reads the refresh token *before* step 3 clears it, which is why the order matters.

---

## 5. Authorization model

Three checks, in this order:

1. **Authenticated** — a valid access token. `ICurrentUser.UserId` is populated by middleware.
2. **Scope** — the target resource belongs to a workspace the caller is a member of.
3. **Role** — `Owner` for settings and destructive operations, `Member` for daily work.

```text
Endpoint kind                                Requirement
────────────────────────────────────────     ─────────────
Read/write issues, comments, labels          Member of the workspace
Create project                               Member
Project/workspace settings, members          Owner
Delete workspace / transfer ownership        Owner
Publish release                              Member
Environments create/update/delete            Owner
Webhooks                                     HMAC signature, no user token
```

Scope resolution happens in a single query in the Application layer:

```csharp
// null → 404 (not 403): never confirm that a resource exists to a non-member
Task<WorkspaceAccess?> GetAccessForIssue(Guid issueId, Guid userId, CancellationToken ct);
record WorkspaceAccess(Guid WorkspaceId, Guid ProjectId, string Role);
```

Anti-patterns to avoid:

- ❌ Trusting `workspaceId` from the request body — always derive it from the resource.
- ❌ Checking authorization only in the endpoint — handlers can be reached from jobs and tests.
- ❌ Returning `403` for a non-member — it confirms the resource exists.

---

## 6. Endpoint protection

- Protected by default via a **fallback policy** (`RequireAuthenticatedUser`); public endpoints
  opt out explicitly with `.AllowAnonymous()` (`/api/auth/*`, `/health*`, webhooks).
  - DECISION (DEVHUB-018): two layers. The `/api` group's `RequireAuthorization()` marks every
    API endpoint explicitly; the fallback policy catches anything with no metadata at all —
    including routes mapped outside `/api`. Forgetting a marking fails closed: a public endpoint
    breaks, a private one never opens.
  - The fallback also applies when **no endpoint matched**: an anonymous request for an unknown
    route gets `401`, not `404`, so routes are not discoverable without a token. Authenticated,
    it is a normal `404`.
  - `EndpointProtectionTests` enumerates every mapped endpoint. Each must be either anonymous or
    carry authorization metadata, **and** the anonymous ones must match an explicit allow-list
    in the test. Under a fallback policy the real leak is an `AllowAnonymous()` inherited from a
    group (a new route inside `/api/auth`), and only the allow-list catches that. Making an
    endpoint public means editing that list.
- The framework's own `401` (no/invalid/expired token) and `403` (policy failed) are
  ProblemDetails with `type` `…/errors/auth.unauthenticated` and `…/errors/auth.forbidden`
  (`CustomizeProblemDetails`). The body is generic; the reason is in JwtBearer's standard
  `WWW-Authenticate` header (`error="invalid_token"`). A `401` an endpoint returns on purpose
  keeps its own type (e.g. `auth.invalid_refresh_token`).
- `ICurrentUser` (Infrastructure `CurrentUser`, scoped, over `IHttpContextAccessor`) reads the
  `sub` claim. `UserId` is null and `IsAuthenticated` false for an anonymous request, outside a
  request (background jobs), or if `sub` is not a Guid. The two properties never disagree.
- Webhooks are anonymous to ASP.NET but authenticated by HMAC signature — see
  [`webhooks-spec.md`](webhooks-spec.md).
- Rate limits: `POST /api/auth/login` and `/register` 10 requests/minute/IP, plus a per-account
  lockout of 5 failed logins in 15 minutes (returns `429` with `Retry-After`, not a permanent
  lock).
  - The IP limit is one fixed-window bucket shared by both endpoints (`RateLimitingOptions`,
    configurable as `RateLimiting:AuthPermitLimit`). It partitions by `RemoteIpAddress`, which
    behind a load balancer is the balancer's address until forwarded headers are configured
    (EPIC 16).
  - DECISION (DEVHUB-016, extended to `/logout` in DEVHUB-017): `POST /api/auth/refresh` is **outside** that bucket
    (`DisableRateLimiting`). The limit exists to slow password guessing; a 256-bit token cannot
    be guessed, and every user behind one NAT refreshing every 15 minutes would otherwise spend
    each other's login attempts.
  - The lockout is a sliding window keyed by the normalized email **whether or not the account
    exists**, so a lockout reveals nothing about which emails are registered.
  - DECISION (DEVHUB-015): lockout state is **in process memory** (`InMemoryLoginThrottle`
    behind the `ILoginThrottle` port). Exact with one API instance; with N instances an attacker
    gets up to N × 5 attempts per window, and a restart clears it. Move it to a table behind the
    same port when the API scales out.

---

## 7. Security headers & transport

```text
Strict-Transport-Security: max-age=31536000; includeSubDomains   (production only)
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: no-referrer
Content-Security-Policy: set on the CloudFront-served SPA (DEVHUB-098)
```

HTTPS everywhere; HTTP redirects to HTTPS. CORS uses an explicit per-environment origin
allow-list — never `AllowAnyOrigin()` together with credentials.

---

## 8. Out of scope for the MVP (each becomes its own ticket)

- Email verification and password reset by email (the `/forgot-password` screen exists but is
  wired in a post-MVP ticket — do not ship a fake flow).
- GitHub OAuth login.
- Two-factor authentication.
- Per-project roles beyond membership.
- Session/device listing and remote revocation.

---

## 9. Test checklist (DEVHUB-013 … 022)

- [ ] Register rejects duplicate email with `409` and a weak password with `400`.
- [ ] Login with a wrong password returns `401` with the same message as an unknown email.
- [ ] An expired access token returns `401`; the client refresh flow recovers transparently.
- [ ] A rotated refresh token cannot be used twice; reuse revokes the chain.
- [ ] Logout invalidates the refresh token.
- [ ] A private endpoint without a token returns `401`.
- [ ] A non-member requesting another workspace's issue gets `404`, not `403`.
- [ ] A `Member` calling an owner-only endpoint gets `403`.
- [ ] No response body anywhere contains `passwordHash` or a raw refresh token.
