import * as SplashScreen from 'expo-splash-screen';
import { StatusBar } from 'expo-status-bar';

import { RootNavigator } from './navigation/RootNavigator';
import { Providers } from './providers';

// Keep the native splash up through the auth bootstrap (SecureStore read → refresh → /api/me).
// RootNavigator hides it once the navigator is ready, so a signed-in user never sees a flash of
// the Welcome screen on cold start. Called at module scope: it must run before the first render.
void SplashScreen.preventAutoHideAsync();

export default function App() {
  return (
    <Providers>
      <RootNavigator />
      <StatusBar style="auto" />
    </Providers>
  );
}
