import { StatusBar } from 'expo-status-bar';

import { RootNavigator } from './navigation/RootNavigator';
import { Providers } from './providers';

export default function App() {
  return (
    <Providers>
      <RootNavigator />
      <StatusBar style="auto" />
    </Providers>
  );
}
