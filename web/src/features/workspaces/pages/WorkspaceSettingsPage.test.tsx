import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it } from 'vitest';
import { AppShell } from '../../../app/layouts/AppShell';
import { WorkspaceLayout } from '../../../app/layouts/WorkspaceLayout';
import { renderWithProviders } from '../../../test/renderWithProviders';
import type { Workspace } from '../types';
import { WorkspaceSettingsPage } from './WorkspaceSettingsPage';

// DEVHUB-028: an owner can rename; a member sees the same information read-only, never a
// disabled or broken form (the ticket's own technical note).

const API = import.meta.env.VITE_API_URL;

function workspace(role: Workspace['role'], name = 'Acme'): Workspace {
  return { id: 'ws-1', name, slug: 'acme', role, memberCount: 2, createdAt: '2026-10-01T00:00:00Z' };
}

let currentWorkspace: Workspace;
let renameStatus: number;

const server = setupServer(
  http.get(`${API}/api/workspaces`, () => HttpResponse.json([currentWorkspace])),
  http.patch(`${API}/api/workspaces/:workspaceId`, async ({ request }) => {
    if (renameStatus === 400) {
      return HttpResponse.json(
        { type: 'https://devhub.dev/errors/validation', title: 'Bad Request', status: 400, errors: { name: ['Name must be 80 characters or fewer.'] } },
        { status: 400 },
      );
    }
    const { name } = (await request.json()) as { name: string };
    currentWorkspace = { ...currentWorkspace, name };
    return HttpResponse.json(currentWorkspace);
  }),
);

const routes = [
  {
    element: <AppShell />,
    children: [
      {
        path: 'w/:workspaceSlug',
        element: <WorkspaceLayout />,
        children: [{ path: 'settings', element: <WorkspaceSettingsPage /> }],
      },
    ],
  },
];

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterAll(() => server.close());
afterEach(() => server.resetHandlers());
beforeEach(() => {
  renameStatus = 200;
});

describe('as an owner', () => {
  beforeEach(() => {
    currentWorkspace = workspace('Owner');
  });

  it('can rename the workspace', async () => {
    const user = userEvent.setup();
    renderWithProviders(routes, '/w/acme/settings');

    const nameInput = await screen.findByLabelText('Name');
    expect(nameInput).toHaveValue('Acme');

    await user.clear(nameInput);
    await user.type(nameInput, 'Acme Corp');
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByText('Saved.')).toBeInTheDocument();
    expect(nameInput).toHaveValue('Acme Corp');
  });

  it('shows the slug as a fixed address with an explanation', async () => {
    renderWithProviders(routes, '/w/acme/settings');

    expect(await screen.findByText('/w/acme')).toBeInTheDocument();
    expect(screen.getByText(/never changes/)).toBeInTheDocument();
  });

  it('disables Save until the name actually changes', async () => {
    renderWithProviders(routes, '/w/acme/settings');
    await screen.findByLabelText('Name');

    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled();
  });

  it('shows the server validation message on a 400', async () => {
    renameStatus = 400;
    const user = userEvent.setup();
    renderWithProviders(routes, '/w/acme/settings');
    const nameInput = await screen.findByLabelText('Name');

    await user.type(nameInput, ' Corp');
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(await screen.findByText('Name must be 80 characters or fewer.')).toBeInTheDocument();
  });
});

describe('as a plain member', () => {
  beforeEach(() => {
    currentWorkspace = workspace('Member');
  });

  it('shows a read-only view instead of a form', async () => {
    renderWithProviders(routes, '/w/acme/settings');

    expect(await screen.findByText('Only a workspace owner can rename this workspace.')).toBeInTheDocument();
    expect(screen.getByText('/w/acme')).toBeInTheDocument();
    expect(screen.queryByLabelText('Name')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
  });
});
