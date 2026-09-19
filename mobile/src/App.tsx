import { StatusBar } from 'expo-status-bar';
import { Providers } from './providers';
import { RootNavigator } from './navigation/RootNavigator';

export default function App() {
  return (
    <Providers>
      <RootNavigator />
      <StatusBar style="auto" />
    </Providers>
  );
}
