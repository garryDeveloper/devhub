import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, userEvent } from '@testing-library/react-native';
import * as SecureStore from 'expo-secure-store';
import type { ReactNode } from 'react';
import { Linking } from 'react-native';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import { RootNavigator } from './RootNavigator';
import { AuthProvider } from '../features/auth/context/AuthProvider';
import { setAccessToken } from '../shared/api/authToken';
import { installFakeApi } from '../test/fakeApi';
import type { FakeApi } from '../test/fakeApi';

// expo-linking reads the Expo manifest to build the dev-client prefix, and there is none under
// Jest. The `devhub://` prefix in linking.ts is what these tests exercise.
jest.mock('expo-linking', () => ({ createURL: () => 'exp://test/' }));

const REFRESH_KEY = 'devhub.refreshToken';
// Only rendered by the Home tab's placeholder — proof that AppTabs is showing.
const HOME_TEXT = 'This screen is built in its own ticket.';

const safeAreaMetrics = {
  frame: { x: 0, y: 0, width: 390, height: 844 },
  insets: { top: 0, left: 0, right: 0, bottom: 0 },
};

function TestProviders({ children }: { children: ReactNode }) {
  const queryClient = new QueryClient();
  return (
    <SafeAreaProvider initialMetrics={safeAreaMetrics}>
      <QueryClientProvider client={queryClient}>
        <AuthProvider>{children}</AuthProvider>
      </QueryClientProvider>
    </SafeAreaProvider>
  );
}

async function renderApp() {
  await render(<RootNavigator />, { wrapper: TestProviders });
}

describe('RootNavigator auth gate', () => {
  let api: FakeApi;

  beforeEach(() => {
    jest.requireMock<{ __reset: () => void }>('expo-secure-store').__reset();
    setAccessToken(null);
    api = installFakeApi();
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  it('shows the AuthStack when no refresh token is stored', async () => {
    await renderApp();

    expect(
      await screen.findByRole('heading', { name: 'DevHub' }),
    ).toBeOnTheScreen();
    expect(api.calls['/api/auth/refresh']).toBeUndefined();
  });

  it('restores the session into AppTabs from a stored refresh token', async () => {
    await SecureStore.setItemAsync(REFRESH_KEY, 'refresh-valid');

    await renderApp();

    expect(await screen.findByText(HOME_TEXT)).toBeOnTheScreen();
    expect(api.calls['/api/auth/refresh']).toBe(1);
  });

  it('falls back to the AuthStack with a notice when the refresh token has expired', async () => {
    await SecureStore.setItemAsync(REFRESH_KEY, 'refresh-expired');

    await renderApp();

    expect(await screen.findByRole('status')).toHaveTextContent(
      'Your session expired. Please log in again.',
    );
    expect(await SecureStore.getItemAsync(REFRESH_KEY)).toBeNull();
  });

  it('keeps the entered email and shows the server message on a failed login', async () => {
    const user = userEvent.setup();
    await renderApp();

    await user.press(await screen.findByRole('button', { name: 'Log in' }));
    await user.type(await screen.findByLabelText('Email'), 'dario@example.com');
    await user.type(screen.getByLabelText('Password'), 'wrong-password');
    await user.press(screen.getByRole('button', { name: 'Log in' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Invalid email or password.',
    );
    expect(screen.getByLabelText('Email')).toHaveDisplayValue(
      'dario@example.com',
    );
  });

  it('logs in into AppTabs and stores the refresh token in SecureStore', async () => {
    const user = userEvent.setup();
    await renderApp();

    await user.press(await screen.findByRole('button', { name: 'Log in' }));
    await user.type(await screen.findByLabelText('Email'), 'dario@example.com');
    await user.type(screen.getByLabelText('Password'), 'correct-password');
    await user.press(screen.getByRole('button', { name: 'Log in' }));

    expect(await screen.findByText(HOME_TEXT)).toBeOnTheScreen();
    expect(await SecureStore.getItemAsync(REFRESH_KEY)).toBe('refresh-valid');
  });

  it('logs out to Welcome, revokes the token and clears SecureStore', async () => {
    const user = userEvent.setup();
    await SecureStore.setItemAsync(REFRESH_KEY, 'refresh-valid');
    await renderApp();
    await screen.findByText(HOME_TEXT);

    // Bottom tab buttons are announced as "Me, tab, 4 of 4".
    await user.press(screen.getByRole('button', { name: /^Me, tab/ }));
    await user.press(await screen.findByRole('button', { name: 'Log out' }));

    expect(
      await screen.findByRole('heading', { name: 'DevHub' }),
    ).toBeOnTheScreen();
    // AppTabs is no longer defined, so there is nothing for the back gesture to return to.
    expect(screen.queryByText(HOME_TEXT)).not.toBeOnTheScreen();
    expect(await SecureStore.getItemAsync(REFRESH_KEY)).toBeNull();
    expect(api.logoutBodies).toEqual([{ refreshToken: 'refresh-valid' }]);
  });

  it('replays a deep link that arrived while signed out once the user logs in', async () => {
    const user = userEvent.setup();
    jest
      .spyOn(Linking, 'getInitialURL')
      .mockResolvedValue('devhub://issues/DEV-42');
    await renderApp();

    await user.press(await screen.findByRole('button', { name: 'Log in' }));
    await user.type(await screen.findByLabelText('Email'), 'dario@example.com');
    await user.type(screen.getByLabelText('Password'), 'correct-password');
    await user.press(screen.getByRole('button', { name: 'Log in' }));

    expect(await screen.findByText('issueKey: DEV-42')).toBeOnTheScreen();
  });
});
