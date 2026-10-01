import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import { useParams } from 'react-router-dom';
import {
  afterAll,
  afterEach,
  beforeAll,
  beforeEach,
  describe,
  expect,
  it,
} from 'vitest';
import {
  getLastWorkspaceSlug,
  setLastWorkspaceSlug,
} from '../features/workspaces/lib/lastWorkspace';
import type { Workspace } from '../features/workspaces/types';
import { renderWithProviders, testUser } from '../test/renderWithProviders';
import { AppShell } from './layouts/AppShell';
import { WorkspaceLayout } from './layouts/WorkspaceLayout';
import { HomeRedirect } from './routes/HomeRedirect';

// DEVHUB-027 end to end in the real shell: `/` decides where to go, the switcher moves between
// workspaces, and the create modal lands the user inside the new one. The API is MSW.

const API = import.meta.env.VITE_API_URL;

const workspace = (
  slug: string,
  name: string,
  role: Workspace['role'] = 'Owner',
): Workspace => ({
  id: `id-${slug}`,
  name,
  slug,
  role,
  memberCount: 1,
  createdAt: '2026-10-01T00:00:00Z',
});

const ACME = workspace('acme', 'Acme');
const GLOBEX = workspace('globex', 'Globex', 'Member');

let serverWorkspaces: Workspace[] = [];
let failList = false;
let createdNames: string[] = [];

const server = setupServer(
  http.get(`${API}/api/workspaces`, () =>
    failList
      ? HttpResponse.json({ title: 'Boom', status: 500 }, { status: 500 })
      : HttpResponse.json(serverWorkspaces),
  ),
  http.post(`${API}/api/workspaces`, async ({ request }) => {
    const { name } = (await request.json()) as { name: string };
    createdNames.push(name);
    if (name === 'Acme') {
      return HttpResponse.json(
        {
          type: 'https://devhub.dev/errors/workspaces.slug_taken',
          title: 'Conflict',
          status: 409,
        },
        { status: 409 },
      );
    }
    const created = workspace('acme-corp', name);
    serverWorkspaces = [...serverWorkspaces, created];
    return HttpResponse.json(created, { status: 201 });
  }),
);

function WorkspaceDashboard() {
  const { workspaceSlug } = useParams();
  return <h1>Dashboard of {workspaceSlug}</h1>;
}

const routes = [
  {
    element: <AppShell />,
    children: [
      { index: true, element: <HomeRedirect /> },
      {
        path: 'w/:workspaceSlug',
        element: <WorkspaceLayout />,
        children: [{ index: true, element: <WorkspaceDashboard /> }],
      },
    ],
  },
];

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterAll(() => server.close());
afterEach(() => server.resetHandlers());
beforeEach(() => {
  localStorage.clear();
  serverWorkspaces = [ACME, GLOBEX];
  failList = false;
  createdNames = [];
});

describe('landing on /', () => {
  it('takes a returning user to the workspace they last used', async () => {
    setLastWorkspaceSlug(testUser.id, 'globex');

    const { router } = renderWithProviders(routes, '/');

    expect(
      await screen.findByRole('heading', { name: 'Dashboard of globex' }),
    ).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/w/globex');
  });

  it('falls back to the first workspace when the remembered one is gone', async () => {
    setLastWorkspaceSlug(testUser.id, 'left-long-ago');

    const { router } = renderWithProviders(routes, '/');

    expect(
      await screen.findByRole('heading', { name: 'Dashboard of acme' }),
    ).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/w/acme');
  });

  it('shows onboarding inside the shell for a user with no workspaces', async () => {
    serverWorkspaces = [];

    renderWithProviders(routes, '/');

    expect(
      await screen.findByText('Create your first workspace'),
    ).toBeInTheDocument();
    // The shell is intact: the switcher is there, and no link points into a missing workspace.
    expect(
      screen.getByRole('button', { name: /No workspace/ }),
    ).toBeInTheDocument();
    expect(
      screen.queryByRole('link', { name: /Overview/ }),
    ).not.toBeInTheDocument();
  });

  it('shows an error with a working Retry when the list fails', async () => {
    failList = true;
    const user = userEvent.setup();

    renderWithProviders(routes, '/');

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('Could not load your workspaces.');

    failList = false;
    await user.click(within(alert).getByRole('button', { name: 'Retry' }));

    expect(
      await screen.findByRole('heading', { name: 'Dashboard of acme' }),
    ).toBeInTheDocument();
  });
});

describe('workspace routes', () => {
  it('shows "not found" for a slug the user does not belong to, and does not remember it', async () => {
    renderWithProviders(routes, '/w/someone-elses');

    expect(await screen.findByText('Workspace not found')).toBeInTheDocument();
    expect(getLastWorkspaceSlug(testUser.id)).toBeNull();
  });
});

describe('switcher', () => {
  it('lists the workspaces with the user’s role in each', async () => {
    const user = userEvent.setup();
    renderWithProviders(routes, '/w/acme');
    await screen.findByRole('heading', { name: 'Dashboard of acme' });

    await user.click(screen.getByRole('button', { name: /Workspace: Acme/ }));

    const list = screen.getByRole('list', { name: 'Your workspaces' });
    expect(within(list).getByRole('link', { name: /Acme/ })).toHaveTextContent(
      'Owner',
    );
    expect(within(list).getByRole('link', { name: /Acme/ })).toHaveAttribute(
      'aria-current',
      'page',
    );
    expect(
      within(list).getByRole('link', { name: /Globex/ }),
    ).toHaveTextContent('Member');
  });

  it('switches workspace client-side and remembers the choice', async () => {
    const user = userEvent.setup();
    const { router } = renderWithProviders(routes, '/w/acme');
    await screen.findByRole('heading', { name: 'Dashboard of acme' });
    const sidebarSwitcher = screen.getByRole('button', {
      name: /Workspace: Acme/,
    });

    await user.click(sidebarSwitcher);
    await user.click(screen.getByRole('link', { name: /Globex/ }));

    expect(
      await screen.findByRole('heading', { name: 'Dashboard of globex' }),
    ).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/w/globex');
    // Same DOM node: the shell was not torn down and rebuilt, i.e. no page reload.
    expect(sidebarSwitcher).toBeInTheDocument();
    expect(sidebarSwitcher).toHaveAccessibleName(/Workspace: Globex/);
    await waitFor(() =>
      expect(getLastWorkspaceSlug(testUser.id)).toBe('globex'),
    );
  });

  it('closes on Escape and returns focus to its button', async () => {
    const user = userEvent.setup();
    renderWithProviders(routes, '/w/acme');
    await screen.findByRole('heading', { name: 'Dashboard of acme' });
    const button = screen.getByRole('button', { name: /Workspace: Acme/ });

    await user.click(button);
    expect(button).toHaveAttribute('aria-expanded', 'true');
    await user.keyboard('{Escape}');

    expect(button).toHaveAttribute('aria-expanded', 'false');
    expect(button).toHaveFocus();
  });
});

describe('creating a workspace', () => {
  it('previews the address and lands inside the new workspace', async () => {
    serverWorkspaces = [];
    const user = userEvent.setup();
    const { router } = renderWithProviders(routes, '/');

    await user.click(
      await screen.findByRole('button', { name: 'Create workspace' }),
    );
    const dialog = screen.getByRole('dialog', { name: 'Create workspace' });
    await user.type(within(dialog).getByLabelText('Name'), 'Acme Corp');

    expect(within(dialog).getByText('/w/acme-corp')).toBeInTheDocument();

    await user.click(
      within(dialog).getByRole('button', { name: 'Create workspace' }),
    );

    expect(
      await screen.findByRole('heading', { name: 'Dashboard of acme-corp' }),
    ).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/w/acme-corp');
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('shows a taken address on the name field', async () => {
    const user = userEvent.setup();
    renderWithProviders(routes, '/w/globex');
    await screen.findByRole('heading', { name: 'Dashboard of globex' });

    await user.click(screen.getByRole('button', { name: /Workspace: Globex/ }));
    await user.click(
      screen.getByRole('button', { name: '+ Create workspace' }),
    );
    const dialog = screen.getByRole('dialog', { name: 'Create workspace' });
    await user.type(within(dialog).getByLabelText('Name'), 'Acme');
    await user.click(
      within(dialog).getByRole('button', { name: 'Create workspace' }),
    );

    expect(
      await within(dialog).findByText(
        'The address /w/acme is already taken. Choose a different name.',
      ),
    ).toBeInTheDocument();
  });

  it('rejects a name with no usable address before calling the API', async () => {
    const user = userEvent.setup();
    renderWithProviders(routes, '/w/acme');
    await screen.findByRole('heading', { name: 'Dashboard of acme' });

    await user.click(screen.getByRole('button', { name: /Workspace: Acme/ }));
    await user.click(
      screen.getByRole('button', { name: '+ Create workspace' }),
    );
    const dialog = screen.getByRole('dialog', { name: 'Create workspace' });
    await user.type(within(dialog).getByLabelText('Name'), '東京チーム');
    await user.click(
      within(dialog).getByRole('button', { name: 'Create workspace' }),
    );

    expect(
      await within(dialog).findByText(/Use at least 3 letters or digits/),
    ).toBeInTheDocument();
    expect(createdNames).toEqual([]);
  });
});
