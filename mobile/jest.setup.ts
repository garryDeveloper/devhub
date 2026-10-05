// Jest setup for the mobile app (DEVHUB-022).

import { notifyManager } from '@tanstack/react-query';

// Fixed base URL so the fake API in tests matches regardless of a developer's .env.local.
process.env.EXPO_PUBLIC_API_URL = 'http://api.test';

// TanStack Query schedules its batched re-renders via `setTimeout(cb, 0)` by default — a real
// macrotask, which can land in a gap between RNTL's act() scopes and trip a spurious "not
// wrapped in act" warning once a query sits inside the tree under test (DEVHUB-029, first query
// hook exercised by a full-navigator test). A microtask lands inside whatever `await` already
// has React's act() scope open, same fix React Query's own docs give for this under RN/Jest.
notifyManager.setScheduler(queueMicrotask);

// expo-secure-store talks to the Keychain/Keystore, which don't exist under Jest. An in-memory
// map with the same async API lets tests assert where a token actually ended up.
jest.mock('expo-secure-store', () => {
  const store = new Map<string, string>();
  return {
    getItemAsync: jest.fn(async (key: string) => store.get(key) ?? null),
    setItemAsync: jest.fn(async (key: string, value: string) => {
      store.set(key, value);
    }),
    deleteItemAsync: jest.fn(async (key: string) => {
      store.delete(key);
    }),
    __reset: () => store.clear(),
  };
});

jest.mock('expo-splash-screen', () => ({
  preventAutoHideAsync: jest.fn(async () => true),
  hideAsync: jest.fn(async () => true),
}));

// AsyncStorage's own test double — the library ships this for exactly this purpose.
jest.mock('@react-native-async-storage/async-storage', () =>
  require('@react-native-async-storage/async-storage/jest/async-storage-mock'),
);
