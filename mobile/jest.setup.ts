// Jest setup for the mobile app (DEVHUB-022).

// Fixed base URL so the fake API in tests matches regardless of a developer's .env.local.
process.env.EXPO_PUBLIC_API_URL = 'http://api.test';

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
