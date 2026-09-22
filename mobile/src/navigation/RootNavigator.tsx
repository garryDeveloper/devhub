import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';

import { AppTabs } from './AppTabs';
import { linking } from './linking';
import { PlaceholderScreen } from './screens/PlaceholderScreen';
import type { AuthStackParamList, RootStackParamList } from './types';

const Root = createNativeStackNavigator<RootStackParamList>();
const Auth = createNativeStackNavigator<AuthStackParamList>();

function AuthStack() {
  return (
    <Auth.Navigator>
      <Auth.Screen name="Welcome" options={{ title: 'Welcome' }}>
        {() => <PlaceholderScreen title="Welcome" />}
      </Auth.Screen>
      <Auth.Screen name="Login" options={{ title: 'Log in' }}>
        {() => <PlaceholderScreen title="Log in" />}
      </Auth.Screen>
      <Auth.Screen name="Register" options={{ title: 'Register' }}>
        {() => <PlaceholderScreen title="Register" />}
      </Auth.Screen>
    </Auth.Navigator>
  );
}

// DEVHUB-022 wires the real session check (SecureStore refresh token →
// POST /api/auth/refresh → GET /api/me). Hardcoded true so this shell's tabs
// and deep links are reachable without auth logic.
const isAuthenticated = true;

export function RootNavigator() {
  return (
    <NavigationContainer linking={linking}>
      <Root.Navigator screenOptions={{ headerShown: false }}>
        {isAuthenticated ? (
          <Root.Screen name="App" component={AppTabs} />
        ) : (
          <Root.Screen name="Auth" component={AuthStack} />
        )}
      </Root.Navigator>
    </NavigationContainer>
  );
}
