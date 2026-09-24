import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import * as SplashScreen from 'expo-splash-screen';

import { AppTabs } from './AppTabs';
import { linking } from './linking';
import type { AuthStackParamList, RootStackParamList } from './types';
import { useAuth } from '../features/auth/hooks/useAuth';
import { LoginScreen } from '../features/auth/screens/LoginScreen';
import { RegisterScreen } from '../features/auth/screens/RegisterScreen';
import { WelcomeScreen } from '../features/auth/screens/WelcomeScreen';

const Root = createNativeStackNavigator<RootStackParamList>();
const Auth = createNativeStackNavigator<AuthStackParamList>();

function AuthStack() {
  return (
    <Auth.Navigator>
      <Auth.Screen
        name="Welcome"
        component={WelcomeScreen}
        options={{ headerShown: false }}
      />
      <Auth.Screen
        name="Login"
        component={LoginScreen}
        options={{ title: 'Log in' }}
      />
      <Auth.Screen
        name="Register"
        component={RegisterScreen}
        options={{ title: 'Register' }}
      />
    </Auth.Navigator>
  );
}

// Auth drives navigation (mobile-architecture.md §3): only one of `App` / `Auth` exists at a
// time. Logging in or out doesn't navigate — it changes which screen is *defined*, so after
// logout there is no AppTabs route left for the back gesture to return to.
//
// Deep link replay (DEVHUB-022 technical notes): a link like devhub://issues/DEV-42 that arrives
// while signed out targets `App`, which isn't defined, so React Navigation shows `Auth` instead.
// `UNSTABLE_routeNamesChangeBehavior="lastUnhandled"` remembers that unhandled state and restores
// it once `App` appears after login. It is marked UNSTABLE in @react-navigation/core 7.x — if a
// minor upgrade renames or removes it, the replay test in RootNavigator.test.tsx fails.
//
// This only renders after AuthProvider's bootstrap, so the native splash is hidden when the
// container is ready: the first frame the user sees is already the right stack.
export function RootNavigator() {
  const { user } = useAuth();

  return (
    <NavigationContainer
      linking={linking}
      onReady={() => void SplashScreen.hideAsync()}
    >
      <Root.Navigator
        screenOptions={{ headerShown: false }}
        UNSTABLE_routeNamesChangeBehavior="lastUnhandled"
      >
        {user ? (
          <Root.Screen name="App" component={AppTabs} />
        ) : (
          <Root.Screen name="Auth" component={AuthStack} />
        )}
      </Root.Navigator>
    </NavigationContainer>
  );
}
