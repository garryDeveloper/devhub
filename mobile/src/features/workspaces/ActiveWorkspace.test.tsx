import AsyncStorage from '@react-native-async-storage/async-storage';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, userEvent } from '@testing-library/react-native';
import * as SecureStore from 'expo-secure-store';
import type { ReactNode } from 'react';

import { WorkspaceSelectorButton } from './components/WorkspaceSelectorButton';
import { ActiveWorkspaceProvider } from './context/ActiveWorkspaceProvider';
import { RootNavigator } from '../../navigation/RootNavigator';
import { setAccessToken } from '../../shared/api/authToken';
import { installFakeApi } from '../../test/fakeApi';
import type { FakeApi } from '../../test/fakeApi';
import { AuthProvider } from '../auth/context/AuthProvider';

// expo-linking reads the Expo manifest, which doesn't exist under Jest (same workaround as
// RootNavigator.test.tsx).
jest.mock('expo-linking', () => ({ createURL: () => 'exp://test/' }));

const REFRESH_KEY = 'devhub.refreshToken';

// A generous timeout for the first query after render: it waits out the full auth bootstrap
// (SecureStore read → refresh → GET /api/me), each a real await, which can sit close to RNTL's
// default 1000ms on a loaded machine.
const BOOTSTRAP_TIMEOUT = { timeout: 5000 };

function Providers({ children }: { children: ReactNode }) {
  const queryClient = new QueryClient();
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <ActiveWorkspaceProvider>{children}</ActiveWorkspaceProvider>
      </AuthProvider>
    </QueryClientProvider>
  );
}

async function signIn() {
  await SecureStore.setItemAsync(REFRESH_KEY, 'refresh-valid');
}

describe('workspace selector and members (DEVHUB-029)', () => {
  let api: FakeApi;

  beforeEach(async () => {
    jest.requireMock<{ __reset: () => void }>('expo-secure-store').__reset();
    setAccessToken(null);
    await AsyncStorage.clear();
    api = installFakeApi();
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  describe('full app (RootNavigator)', () => {
    // These never open the BottomSheet/Modal selector — that interaction is covered in its own
    // lighter-weight describe block below, so the slide animation's timers never compete with
    // the auth-bootstrap chain this suite already exercises.
    async function renderApp() {
      return await render(<RootNavigator />, { wrapper: Providers });
    }

    async function goToProjectsTab(user: ReturnType<typeof userEvent.setup>) {
      await user.press(
        await screen.findByRole(
          'button',
          { name: /^Projects, tab/ },
          BOOTSTRAP_TIMEOUT,
        ),
      );
    }

    async function openWorkspaceMembers(
      user: ReturnType<typeof userEvent.setup>,
    ) {
      await user.press(
        await screen.findByRole(
          'button',
          { name: /^Me, tab/ },
          BOOTSTRAP_TIMEOUT,
        ),
      );
      await user.press(await screen.findByText('Workspace members'));
    }

    it('defaults to the first workspace and lists its members with no mutation controls', async () => {
      await signIn();
      const user = userEvent.setup();
      await renderApp();
      await goToProjectsTab(user);

      expect(
        await screen.findByRole('button', { name: 'Workspace: Acme' }),
      ).toBeOnTheScreen();

      await openWorkspaceMembers(user);

      expect(await screen.findByText('Dario')).toBeOnTheScreen();
      expect(screen.getByText('Owner')).toBeOnTheScreen();
      expect(
        screen.queryByRole('button', { name: /invite/i }),
      ).not.toBeOnTheScreen();
      expect(
        screen.queryByRole('button', { name: /remove/i }),
      ).not.toBeOnTheScreen();
    });

    it('restores a previously selected workspace after a cold start', async () => {
      // Simulates a user who picked "Beta Co" on a prior run: the id is already in AsyncStorage
      // before anything mounts, same as it would be after an app restart.
      await AsyncStorage.setItem('devhub:activeWorkspace:1', 'ws-2');
      await signIn();
      const user = userEvent.setup();
      await renderApp();
      await goToProjectsTab(user);

      expect(
        await screen.findByRole('button', { name: 'Workspace: Beta Co' }),
      ).toBeOnTheScreen();

      await openWorkspaceMembers(user);

      expect(await screen.findByText('Other')).toBeOnTheScreen();
      expect(api.calls['/api/workspaces/ws-2/members']).toBe(1);
    });

    it('shows an empty state pointing to the web app when the user has no workspace', async () => {
      api.override('/api/workspaces', () =>
        new Response(JSON.stringify([]), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        }),
      );
      await signIn();
      const user = userEvent.setup();
      await renderApp();

      await openWorkspaceMembers(user);

      expect(await screen.findByText('No workspace yet')).toBeOnTheScreen();
      expect(screen.getByText(/DevHub web app/)).toBeOnTheScreen();
    });
  });

  describe('WorkspaceSelectorButton', () => {
    // Mounted on its own (no RootNavigator/AppTabs) so the BottomSheet's slide animation is the
    // only thing in flight — it still needs AuthProvider + ActiveWorkspaceProvider underneath,
    // since the button reads the active workspace from context.
    async function renderButton() {
      return await render(<WorkspaceSelectorButton />, { wrapper: Providers });
    }

    it('switching workspaces persists the new selection and updates the button label', async () => {
      await signIn();
      const user = userEvent.setup();
      await renderButton();

      await user.press(
        await screen.findByRole('button', { name: 'Workspace: Acme' }),
      );
      await user.press(
        await screen.findByRole('button', { name: 'Beta Co, Member' }),
      );

      expect(
        await screen.findByRole('button', { name: 'Workspace: Beta Co' }),
      ).toBeOnTheScreen();
      expect(await AsyncStorage.getItem('devhub:activeWorkspace:1')).toBe(
        'ws-2',
      );
    });
  });
});
