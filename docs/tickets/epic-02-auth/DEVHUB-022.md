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

- [x] Build `Welcome`, `Login`, `Register` screens with keyboard-aware layout and inline errors.
      *`features/auth/screens/`. `AuthFormLayout` uses `KeyboardAvoidingView` (`padding` on iOS,
      unset on Android, which already resizes the window) with a `ScrollView`
      (`keyboardShouldPersistTaps="handled"`), so the first tap on the submit button works
      while the keyboard is open. It uses React Native primitives only, no keyboard library.
      `FormField` bridges React Hook Form to `TextInput` through `Controller`, because
      `register()` cannot attach to a native input. Field errors show under each field and
      server errors in a `role="alert"` banner. `schemas.ts` and `lib/applyServerErrors.ts` are
      deliberate copies of the web files; there is no shared package yet.
      Also adds `shared/components/Button.tsx` (≥ 44pt touch target, busy state).*
- [x] Store the refresh token with `expo-secure-store`; keep the access token in memory.
      *`shared/api/refreshTokenStorage.ts` (key `devhub.refreshToken`) and
      `shared/api/authToken.ts`. SecureStore is **async**, unlike web's `localStorage`, so every
      read and write is awaited.*
- [x] `AuthProvider` with the same API as web (`user`, `login`, `register`, `logout`,
      `isBootstrapping`); show the splash screen until bootstrap resolves.
      *`features/auth/context/AuthProvider.tsx` also exposes `sessionExpired`, for parity with
      DEVHUB-021. DECISION (DEVHUB-022): use the **native** splash (`expo-splash-screen`, added
      as a dependency). `App.tsx` calls `preventAutoHideAsync()` at module scope, the
      `AuthProvider` renders nothing while bootstrapping, and `RootNavigator` calls
      `hideAsync()` from `NavigationContainer.onReady`. The first frame the user sees is
      already the right stack, with no React splash in between.*
- [x] `RootNavigator` switches between `AuthStack` and `AppTabs` on auth state.
      *`user ? App : Auth`. Nothing navigates imperatively: login and logout change which root
      screen is **defined**.*
- [x] Port the single-flight refresh logic from DEVHUB-021.
      *`shared/api/client.ts` is a near line-for-line port: `refreshInFlight ??=`, one retry,
      `/api/auth/*` excluded, the rotated-token guard, and logout only on a 401 from
      `/refresh`. The promise is assigned synchronously, before the first `await` of the
      SecureStore read; otherwise concurrent 401s could all pass the "in flight?" check. The
      bootstrap uses `refreshTokenOnce()` as well.*
- [x] Logout clears SecureStore, the query cache and resets navigation.
      *`auth-spec.md` §4 order: await the SecureStore read, fire `POST /logout` without
      awaiting it, clear the in-memory token, `deleteItemAsync`, `queryClient.clear()`, then
      `setUser(null)`, which swaps to `AuthStack` (Welcome). The button is on the Me tab
      (`ProfileScreen`).*
- [x] Tests: bootstrap with no token → AuthStack; with a valid token → AppTabs; expired refresh
      → AuthStack.
      *There was no test runner, so this ticket adds Jest via `jest-expo`, plus
      `@testing-library/react-native` 14 with `test-renderer` 1.2 (RNTL 14 dropped
      `react-test-renderer`; 1.2 is the line for React 19.2). `@types/jest` is also added, and
      `tsconfig.json` sets `"types": ["jest"]` because TypeScript 6 no longer includes `@types/*`
      automatically. `jest.setup.ts` mocks SecureStore with an in-memory map and mocks the
      splash. Network: `src/test/fakeApi.ts` fakes `fetch`, and the real `apiFetch` still runs.
      MSW was not used, because it needs stream polyfills under React Native's Jest.*
      *`navigation/RootNavigator.test.tsx` covers the three bootstrap cases, plus: failed login
      keeps the email; login stores the token in SecureStore; logout returns to Welcome and
      revokes the token; deep link replay after login. `shared/api/client.test.ts` ports the
      DEVHUB-021 cases. Mutation-checked: removing single-flight fails 2 tests, and removing
      `lastUnhandled` fails the replay test.*

## Acceptance criteria

- [x] Closing and reopening the app keeps the user signed in.
      *Covered by "restores the session into AppTabs from a stored refresh token". A real
      cold start on a device or simulator is still the owner's to confirm; none was available
      in this session.*
- [x] Tokens are never written to `AsyncStorage` or logged.
      *`@react-native-async-storage/async-storage` is not a dependency, and `mobile/src` has
      no `console.*` call.*
- [x] Login errors show inline without losing the entered email.
      *`LoginScreen` never resets the form on failure; covered by a test.*
- [x] Logout returns to Welcome and the back gesture cannot re-enter the app.
      *After logout, `App` is no longer a defined route, so there is no history entry to go
      back to. The test asserts Welcome is shown and the AppTabs content is gone. The gesture
      itself still needs a manual check on a device.*

## Technical notes

- `SecureStore` is unavailable on web builds of Expo — if you ever run the app on web, guard the
  import.
  *Done: on `Platform.OS === 'web'`, `refreshTokenStorage.ts` keeps the token in memory. The
  session is lost on reload, which is safer than silently falling back to `localStorage`.*
- Deep links that arrive while unauthenticated must be stored and replayed after login;
  otherwise a notification tap during a signed-out state drops the user on the home screen.
  *DECISION (DEVHUB-022): use React Navigation's built-in
  `UNSTABLE_routeNamesChangeBehavior="lastUnhandled"` on the root navigator
  (`@react-navigation/core` 7.22). It stores the state it could not handle (a link to `App`
  while only `Auth` exists) and restores it when `App` appears after login. **Risk:** the API is
  marked UNSTABLE and could change in a minor release. The test "replays a deep link that
  arrived while signed out" is the tripwire. If it breaks, the fallback is a manual pending-link
  store around `linking.subscribe`/`getInitialURL`.*

## Learning goals

Secure storage on mobile, navigation state driven by auth, cold-start bootstrap, deep link
replay.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §4.
