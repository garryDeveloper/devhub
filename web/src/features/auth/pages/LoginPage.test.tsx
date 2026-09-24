import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactNode } from 'react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../../shared/api/client';
import type { AuthContextValue } from '../context/authContext';
import { AuthContext } from '../context/authContext';
import { LoginPage } from './LoginPage';

function renderLoginPage(
  authOverrides: Partial<AuthContextValue>,
  initialEntry: string,
) {
  const value: AuthContextValue = {
    user: null,
    isBootstrapping: false,
    sessionExpired: false,
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    ...authOverrides,
  };

  const Wrapper = ({ children }: { children: ReactNode }) => (
    <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
  );

  render(
    <Wrapper>
      <MemoryRouter initialEntries={[initialEntry]}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/" element={<div>Overview page</div>} />
          <Route path="/p/DEV/issues" element={<div>Issues page</div>} />
        </Routes>
      </MemoryRouter>
    </Wrapper>,
  );

  return value;
}

describe('LoginPage', () => {
  it('redirects to returnTo after a successful login', async () => {
    const user = userEvent.setup();
    renderLoginPage(
      { login: vi.fn().mockResolvedValue(undefined) },
      '/login?returnTo=%2Fp%2FDEV%2Fissues',
    );

    await user.type(screen.getByLabelText('Email'), 'dario@example.com');
    await user.type(screen.getByLabelText('Password'), 'password123');
    await user.click(screen.getByRole('button', { name: 'Log in' }));

    expect(await screen.findByText('Issues page')).toBeInTheDocument();
  });

  it('redirects to the overview when there is no returnTo', async () => {
    const user = userEvent.setup();
    renderLoginPage(
      { login: vi.fn().mockResolvedValue(undefined) },
      '/login',
    );

    await user.type(screen.getByLabelText('Email'), 'dario@example.com');
    await user.type(screen.getByLabelText('Password'), 'password123');
    await user.click(screen.getByRole('button', { name: 'Log in' }));

    expect(await screen.findByText('Overview page')).toBeInTheDocument();
  });

  it('shows the server message and keeps the entered email on an invalid login', async () => {
    const user = userEvent.setup();
    const failingLogin = vi.fn().mockRejectedValue(
      new ApiError({
        title: 'Authentication failed.',
        status: 401,
        detail: 'Invalid email or password.',
      }),
    );
    renderLoginPage({ login: failingLogin }, '/login');

    const emailInput = screen.getByLabelText('Email');
    await user.type(emailInput, 'dario@example.com');
    await user.type(screen.getByLabelText('Password'), 'wrong-password');
    await user.click(screen.getByRole('button', { name: 'Log in' }));

    expect(await screen.findByText('Invalid email or password.')).toBeInTheDocument();
    expect(emailInput).toHaveValue('dario@example.com');
  });

  it('explains the redirect when the session expired', () => {
    renderLoginPage({}, '/login?expired=1&returnTo=%2Fp%2FDEV%2Fissues');

    expect(screen.getByRole('status')).toHaveTextContent(
      'Your session expired. Please log in again.',
    );
  });

  it('shows no session-expired message on a normal visit', () => {
    renderLoginPage({}, '/login');

    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });
});
