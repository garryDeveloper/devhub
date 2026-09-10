# DEVHUB-009 — Create the React Native application shell

|  |  |
|---|---|
| **Epic** | EPIC 1 — Foundation & project setup |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | M |
| **Depends on** | DEVHUB-001 |
| **Specs** | [`mobile-architecture.md`](../../tech-specs/mobile-architecture.md) |

## Context

Same idea as the web shell: navigation, providers, theme and the shared screen states, so mobile
tickets are about features and not about plumbing.

## Scope

**In:** Expo + TS project, navigation skeleton (auth stack vs tabs), providers, theme, shared
components, API client, deep-link configuration.
**Out:** auth logic (DEVHUB-022), feature screens.

## Tasks

- [ ] `npx create-expo-app mobile --template blank-typescript`; enable strict TS.
- [ ] Install React Navigation (native stack + bottom tabs), TanStack Query, SecureStore,
      React Hook Form, Zod.
- [ ] Create `navigation/`: `RootNavigator` switching between `AuthStack` and `AppTabs`, with
      typed param lists.
- [ ] Create the four tabs (Home, Issues, Projects, Me) with placeholder screens.
- [ ] Add `shared/theme` (colors matching web, spacing, typography) and a `Screen` wrapper with
      safe-area handling.
- [ ] Add `Loading`, `ListEmpty`, `ErrorView`, `BottomSheet` shared components.
- [ ] Port the API client (same error shape as web) reading `EXPO_PUBLIC_API_URL`.
- [ ] Configure the `devhub://` scheme and `linking.ts`.
- [ ] Verify a `/health` call succeeds from a physical device over the LAN.

## Acceptance criteria

- [ ] The app runs on an iOS simulator/device and an Android emulator/device via Expo Go.
- [ ] Tab navigation works; navigation params are type-checked at compile time.
- [ ] A `devhub://projects/123` deep link opens the right placeholder screen.
- [ ] `npm run typecheck` and `npm run lint` pass.

## Technical notes

- `localhost` on a device is the device. Use the LAN IP and make sure the API binds `0.0.0.0`.
- Do not install a heavyweight UI kit. The screens here are simple, and a kit's opinions will
  fight the design later.

## Learning goals

Expo project anatomy, React Navigation structure and typing, deep linking, why mobile needs its
own IA rather than the desktop layout.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §4.
