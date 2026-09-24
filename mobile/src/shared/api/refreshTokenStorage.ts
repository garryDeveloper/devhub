import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

// The refresh token goes to the Keychain (iOS) / Keystore (Android) via expo-secure-store — never
// AsyncStorage, which is plain text on disk (mobile-architecture.md §4, DEVHUB-022).
//
// Unlike web's localStorage, SecureStore is asynchronous: every read is a round trip to the
// native module, so every caller has to await it.
//
// SecureStore has no web implementation. If the app is ever run with `expo start --web`, fall
// back to memory: the session is lost on reload, which is acceptable for a dev-only target and
// safer than silently persisting the token in localStorage.
const STORAGE_KEY = 'devhub.refreshToken';
const isWeb = Platform.OS === 'web';
let webFallback: string | null = null;

export async function getStoredRefreshToken(): Promise<string | null> {
  if (isWeb) return webFallback;
  return SecureStore.getItemAsync(STORAGE_KEY);
}

export async function setStoredRefreshToken(token: string): Promise<void> {
  if (isWeb) {
    webFallback = token;
    return;
  }
  await SecureStore.setItemAsync(STORAGE_KEY, token);
}

export async function clearStoredRefreshToken(): Promise<void> {
  if (isWeb) {
    webFallback = null;
    return;
  }
  await SecureStore.deleteItemAsync(STORAGE_KEY);
}
