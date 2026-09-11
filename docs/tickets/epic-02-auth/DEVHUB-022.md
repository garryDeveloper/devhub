# DEVHUB-022 — Mobile authentication with secure storage

|  |  |
|---|---|
| **Epic** | EPIC 2 — Authentication & user account |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-009, DEVHUB-019 |
| **Specs** | [`mobile-architecture.md`](../../tech-specs/mobile-architecture.md) §4 |

## Context

Same flow as web, different storage rules: a phone's `AsyncStorage` is plain text on disk, so
tokens go to the Keychain/Keystore.

## Scope

**In:** welcome/login/register screens, SecureStore token storage, bootstrap gate, refresh
interception, logout.
**Out:** biometric unlock, social login (both post-MVP).

## Tasks

- [ ] Build `Welcome`, `Login`, `Register` screens with keyboard-aware layout and inline errors.
- [ ] Store the refresh token with `expo-secure-store`; keep the access token in memory.
- [ ] `AuthProvider` with the same API as web (`user`, `login`, `register`, `logout`,
      `isBootstrapping`); show the splash screen until bootstrap resolves.
- [ ] `RootNavigator` switches between `AuthStack` and `AppTabs` on auth state.
- [ ] Port the single-flight refresh logic from DEVHUB-021.
- [ ] Logout clears SecureStore, the query cache and resets navigation.
- [ ] Tests: bootstrap with no token → AuthStack; with a valid token → AppTabs; expired refresh
      → AuthStack.

## Acceptance criteria

- [ ] Closing and reopening the app keeps the user signed in.
- [ ] Tokens are never written to `AsyncStorage` or logged.
- [ ] Login errors show inline without losing the entered email.
- [ ] Logout returns to Welcome and the back gesture cannot re-enter the app.

## Technical notes

- `SecureStore` is unavailable on web builds of Expo — if you ever run the app on web, guard the
  import.
- Deep links that arrive while unauthenticated must be stored and replayed after login;
  otherwise a notification tap during a signed-out state drops the user on the home screen.

## Learning goals

Secure storage on mobile, navigation state driven by auth, cold-start bootstrap, deep link
replay.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §4.
