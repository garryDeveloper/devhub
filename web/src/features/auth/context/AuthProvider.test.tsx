import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { getAccessToken } from '../../../shared/api/authToken';
import * as authApi from '../api/authApi';
import { useAuth } from '../hooks/useAuth';
import { AuthProvider } from './AuthProvider';

vi.mock('../api/authApi');

function TestConsumer() {
  const { user, login, logout } = useAuth();
  return (
    <div>
      <p data-testid="user">{user ? user.displayName : 'anonymous'}</p>
      <button
        onClick={() => {
          void login('dario@example.com', 'password123');
        }}
      >
        Log in
      </button>
      <button
        onClick={() => {
          void logout();
        }}
      >
        Log out
      </button>
    </div>
  );
}

function renderWithProviders() {
  const queryClient = new QueryClient();
  const clearSpy = vi.spyOn(queryClient, 'clear');
  render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>
    </QueryClientProvider>,
  );
  return { clearSpy };
}

describe('AuthProvider', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.mocked(authApi.loginRequest).mockReset();
    vi.mocked(authApi.logoutRequest).mockReset().mockResolvedValue(undefined);
  });

  it('has no session when no refresh token is stored', async () => {
    renderWithProviders();

    await waitFor(() =>
      expect(screen.getByTestId('user')).toHaveTextContent('anonymous'),
    );
  });

  it('sets the user and access token on a successful login', async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.loginRequest).mockResolvedValue({
      accessToken: 'access-123',
      expiresIn: 900,
      refreshToken: 'refresh-123',
      user: {
        id: '1',
        email: 'dario@example.com',
        displayName: 'Dario',
        avatarUrl: null,
      },
    });

    renderWithProviders();
    await waitFor(() =>
      expect(screen.getByTestId('user')).toHaveTextContent('anonymous'),
    );

    await user.click(screen.getByText('Log in'));

    await waitFor(() =>
      expect(screen.getByTestId('user')).toHaveTextContent('Dario'),
    );
    expect(getAccessToken()).toBe('access-123');
    expect(localStorage.getItem('devhub.refreshToken')).toBe('refresh-123');
  });

  it('clears the user, the tokens and the query cache on logout', async () => {
    const user = userEvent.setup();
    vi.mocked(authApi.loginRequest).mockResolvedValue({
      accessToken: 'access-123',
      expiresIn: 900,
      refreshToken: 'refresh-123',
      user: {
        id: '1',
        email: 'dario@example.com',
        displayName: 'Dario',
        avatarUrl: null,
      },
    });

    const { clearSpy } = renderWithProviders();
    await user.click(screen.getByText('Log in'));
    await waitFor(() =>
      expect(screen.getByTestId('user')).toHaveTextContent('Dario'),
    );

    await user.click(screen.getByText('Log out'));

    await waitFor(() =>
      expect(screen.getByTestId('user')).toHaveTextContent('anonymous'),
    );
    expect(getAccessToken()).toBeNull();
    expect(localStorage.getItem('devhub.refreshToken')).toBeNull();
    expect(clearSpy).toHaveBeenCalled();
    expect(authApi.logoutRequest).toHaveBeenCalledWith('refresh-123');
  });
});
