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

Logout revokes the presented refresh token (and its family). It cannot revoke an already-issued
access token — the ≤15 minute window is the accepted trade-off, documented here on purpose.

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

- `[Authorize]` is the default via a fallback policy; public endpoints opt out explicitly with
  `[AllowAnonymous]` (`/api/auth/*`, `/health*`, webhooks).
- Webhooks are anonymous to ASP.NET but authenticated by HMAC signature — see
  [`webhooks-spec.md`](webhooks-spec.md).
- Rate limits: `POST /api/auth/login` and `/register` 10 requests/minute/IP, plus a per-account
  lockout of 5 failed logins in 15 minutes (returns `429` with `Retry-After`, not a permanent
  lock).
  - The IP limit is one fixed-window bucket shared by both endpoints (`RateLimitingOptions`,
    configurable as `RateLimiting:AuthPermitLimit`). It partitions by `RemoteIpAddress`, which
    behind a load balancer is the balancer's address until forwarded headers are configured
    (EPIC 16).
  - DECISION (DEVHUB-016): `POST /api/auth/refresh` is **outside** that bucket
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
