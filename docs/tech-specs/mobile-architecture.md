# Mobile architecture

React Native + TypeScript with Expo. Companion app, not a port of the web UI.

---

## 1. Stack decisions

| Concern | Choice | Why |
|---|---|---|
| Runtime | Expo (managed) | no native toolchain to fight while learning; EAS build later |
| Language | TypeScript, strict | |
| Navigation | React Navigation (native stack + bottom tabs) | typed params, deep linking |
| Server state | TanStack Query (same patterns as web) | one mental model across clients |
| Secure storage | `expo-secure-store` | Keychain / Keystore — **never** AsyncStorage for tokens |
| Forms | React Hook Form + Zod | shared schema style with web |
| Styling | StyleSheet + a small theme module (or NativeWind) | avoid heavy UI kits |
| Lists | `FlatList` / `FlashList` | virtualization by default |
| Tests | Jest + React Native Testing Library for logic and key screens | |

If Expo becomes limiting (custom native module), the escape hatch is a dev client build —
document the decision in a ticket before ejecting.

---

## 2. Folder structure

```text
mobile/src/
├── App.tsx
├── navigation/
│   ├── RootNavigator.tsx        # Auth vs App switch
│   ├── AppTabs.tsx
│   ├── linking.ts               # deep links
│   └── types.ts                 # typed param lists
├── features/
│   ├── auth/
│   ├── workspaces/
│   ├── projects/
│   ├── issues/
│   ├── comments/
│   ├── environments/
│   ├── deployments/
│   ├── releases/
│   ├── cicd/
│   ├── dashboard/
│   ├── search/
│   └── notifications/
├── shared/
│   ├── api/                     # client, queryKeys, error mapping
│   ├── components/              # Screen, ListEmpty, ErrorView, Loading, BottomSheet
│   ├── hooks/
│   ├── theme/                   # colors, spacing, typography
│   └── lib/
└── types/
```

Mirrors the web structure on purpose: the same feature name means the same thing in both apps.

---

## 3. Navigation

```text
RootNavigator
├── AuthStack        (no token)      Welcome · Login · Register
└── AppTabs          (token)         Home · Issues · Projects · Me
```

Typed params:

```ts
export type ProjectsStackParamList = {
  ProjectList: undefined;
  ProjectDetail: { projectId: string; projectKey: string };
  EnvironmentDetail: { environmentId: string };
  DeploymentDetail: { deploymentId: string };
};
```

Deep links (used by notifications):

```text
devhub://issues/DEV-42
devhub://deployments/{deploymentId}
devhub://projects/{projectId}
```

`linking.ts` maps these to screens and handles cold start (app opened from a notification) as
well as warm start.

---

## 4. Auth & secure storage

```text
Login  → POST /api/auth/login
       → access token kept in memory
       → refresh token stored via SecureStore
App start → read refresh token → POST /api/auth/refresh → GET /api/me
          → success: AppTabs   |   failure: AuthStack
```

Rules:

- Tokens never touch `AsyncStorage`, Redux, or logs.
- The API client retries a 401 exactly once after a single-flight refresh, same as web.
- Logout clears SecureStore, the query cache, and resets navigation to `AuthStack`.
- Biometric unlock is post-MVP; do not add it in the MVP tickets.

---

## 5. Data fetching

Same `queryKeys` conventions as web. Mobile-specific rules:

- Every list screen has **pull-to-refresh** (`refetch` + `RefreshControl`).
- `staleTime` is longer than web (60 s) — mobile networks are worse and screens are re-entered
  constantly.
- Detail screens seed the cache from the list item so they render instantly and then refresh.
- Mutations are optimistic for status/priority changes, pessimistic for create/delete.
- Reachability: on network error show `ErrorView` with Retry, never a blank screen.

Offline write queueing is **out of scope for the MVP**. Reads may come from cache; writes require
connectivity and say so.

---

## 6. Screen contract

Every screen uses the `<Screen>` wrapper, which provides safe-area insets, the background, and
consistent padding, and every data screen renders one of:

```text
<Loading />     skeleton or spinner
<ListEmpty />   explanatory copy + primary action
<ErrorView />   message + Retry
content
```

Interaction rules:

- Status and priority changes open a **bottom sheet**, not a dropdown.
- Destructive actions use a native confirmation dialog.
- Touch targets ≥ 44pt; lists have `keyExtractor` and stable item heights where possible.
- Text scales with the system font size — no fixed pixel line heights on body text.

---

## 7. What mobile does and does not do

**Does:** check environment health and deployments, read and filter issues, create and update
issues, comment, receive notifications, view releases and CI/CD runs.

**Does not (stays on web):** workspace and project settings, member management, label
administration, saved filter management, release publishing, drag & drop board editing (board
is read-only, horizontally scrollable).

This split is deliberate: it keeps the mobile app small and useful instead of a bad copy of the
desktop app.

---

## 8. Configuration & builds

```text
app.config.ts  →  extra: { apiUrl, appEnv }
.env.local     →  EXPO_PUBLIC_API_URL=http://192.168.x.x:5080
```

- On a physical device, `localhost` is the phone — use the LAN IP of the dev machine.
- Three profiles in `eas.json`: `development`, `preview` (staging API), `production`.
- No secrets in `app.config.ts` — anything shipped in the bundle is public.
- CI (DEVHUB-103) runs typecheck, lint and tests; store builds are out of scope for the MVP.
