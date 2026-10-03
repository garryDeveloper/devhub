import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { delay, http, HttpResponse } from 'msw';
import { setupServer } from 'msw/node';
import {
  afterAll,
  afterEach,
  beforeAll,
  beforeEach,
  describe,
  expect,
  it,
} from 'vitest';
import { renderWithProviders, testUser } from '../../../test/renderWithProviders';
import { AppShell } from '../../../app/layouts/AppShell';
import { WorkspaceLayout } from '../../../app/layouts/WorkspaceLayout';
import type { Member, Workspace } from '../types';
import { WorkspaceMembersPage } from './WorkspaceMembersPage';

// DEVHUB-028: role-conditional UI, optimistic role changes with rollback, and confirmed
// destructive actions, exercised end to end against a mocked API (same style as
// app/workspaceNavigation.test.tsx).

const API = import.meta.env.VITE_API_URL;

function workspace(role: Workspace['role']): Workspace {
  return { id: 'ws-1', name: 'Acme', slug: 'acme', role, memberCount: 2, createdAt: '2026-10-01T00:00:00Z' };
}

function member(id: string, userId: string, displayName: string, email: string, role: Member['role']): Member {
  return { id, user: { id: userId, email, displayName, avatarUrl: null }, role, joinedAt: '2026-09-01T00:00:00Z' };
}

// Dario is the signed-in test user (renderWithProviders). His membership role must agree with
// `currentWorkspace.role` (both come from the same caller-membership row in real life), so each
// describe block below picks the matching pair.
const DARIO_AS_OWNER = member('member-dario', testUser.id, 'Dario', testUser.email, 'Owner');
const DARIO_AS_MEMBER = member('member-dario', testUser.id, 'Dario', testUser.email, 'Member');
const BOB_AS_MEMBER = member('member-bob', 'user-bob', 'Bob', 'bob@example.com', 'Member');
const BOB_AS_OWNER = member('member-bob', 'user-bob', 'Bob', 'bob@example.com', 'Owner');

let currentWorkspace: Workspace;
let members: Member[];
let lastInviteBody: unknown;
let roleChangeStatus: number;

const server = setupServer(
  http.get(`${API}/api/workspaces`, () => HttpResponse.json([currentWorkspace])),
  http.get(`${API}/api/workspaces/:workspaceId/members`, () => HttpResponse.json(members)),
  http.post(`${API}/api/workspaces/:workspaceId/members`, async ({ request }) => {
    const body = (await request.json()) as { email: string; role: Member['role'] };
    lastInviteBody = body;
    if (body.email === 'ghost@example.com') {
      return HttpResponse.json(
        { type: 'https://devhub.dev/errors/workspaces.user_not_found', title: 'Not Found', status: 404, detail: 'No DevHub account with that email.' },
        { status: 404 },
      );
    }
    if (body.email === BOB_AS_MEMBER.user.email) {
      return HttpResponse.json(
        { type: 'https://devhub.dev/errors/workspaces.already_member', title: 'Conflict', status: 409 },
        { status: 409 },
      );
    }
    const created = member('member-new', 'user-ana', 'Ana', body.email, body.role);
    members = [...members, created];
    return HttpResponse.json(created, { status: 201 });
  }),
  http.patch(`${API}/api/workspaces/:workspaceId/members/:memberId`, async ({ params, request }) => {
    await delay(10);
    if (roleChangeStatus === 422) {
      return HttpResponse.json(
        { type: 'https://devhub.dev/errors/workspaces.last_owner', title: 'Unprocessable', status: 422, detail: 'A workspace must keep at least one owner.' },
        { status: 422 },
      );
    }
    const { role } = (await request.json()) as { role: Member['role'] };
    members = members.map((candidate) => (candidate.id === params.memberId ? { ...candidate, role } : candidate));
    return HttpResponse.json(members.find((candidate) => candidate.id === params.memberId));
  }),
  http.delete(`${API}/api/workspaces/:workspaceId/members/:memberId`, ({ params }) => {
    members = members.filter((candidate) => candidate.id !== params.memberId);
    return new HttpResponse(null, { status: 204 });
  }),
);

const routes = [
  {
    element: <AppShell />,
    children: [
      {
        path: 'w/:workspaceSlug',
        element: <WorkspaceLayout />,
        children: [{ path: 'members', element: <WorkspaceMembersPage /> }],
      },
      { index: true, element: <div>Home</div> },
    ],
  },
];

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterAll(() => server.close());
afterEach(() => server.resetHandlers());
beforeEach(() => {
  lastInviteBody = undefined;
  roleChangeStatus = 200;
});

describe('as an owner', () => {
  beforeEach(() => {
    currentWorkspace = workspace('Owner');
    members = [DARIO_AS_OWNER, BOB_AS_MEMBER];
  });

  it('lists every member with their role, email and joined date', async () => {
    renderWithProviders(routes, '/w/acme/members');

    expect(await screen.findByText('Bob')).toBeInTheDocument();
    const table = screen.getByRole('table');
    expect(within(table).getByText('bob@example.com')).toBeInTheDocument();
    expect(within(table).getByText('Dario')).toBeInTheDocument();
  });

  it('can invite a new member by email', async () => {
    const user = userEvent.setup();
    renderWithProviders(routes, '/w/acme/members');
    await screen.findByText('Bob');

    await user.click(screen.getByRole('button', { name: 'Invite member' }));
    const dialog = screen.getByRole('dialog', { name: 'Invite a member' });
    await user.type(within(dialog).getByLabelText('Email'), 'ana@example.com');
    await user.click(within(dialog).getByRole('button', { name: 'Invite' }));

    await waitFor(() => expect(lastInviteBody).toEqual({ email: 'ana@example.com', role: 'Member' }));
    expect(await screen.findByText('Ana')).toBeInTheDocument();
  });

  it('maps a 404 unknown email onto the email field with the server wording', async () => {
    const user = userEvent.setup();
    renderWithProviders(routes, '/w/acme/members');
    await screen.findByText('Bob');

    await user.click(screen.getByRole('button', { name: 'Invite member' }));
    const dialog = screen.getByRole('dialog', { name: 'Invite a member' });
    await user.type(within(dialog).getByLabelText('Email'), 'ghost@example.com');
    await user.click(within(dialog).getByRole('button', { name: 'Invite' }));

    expect(await within(dialog).findByText('No DevHub account with that email.')).toBeInTheDocument();
  });

  it('maps a 409 already-a-member onto the email field', async () => {
    const user = userEvent.setup();
    renderWithProviders(routes, '/w/acme/members');
    await screen.findByText('Bob');

    await user.click(screen.getByRole('button', { name: 'Invite member' }));
    const dialog = screen.getByRole('dialog', { name: 'Invite a member' });
    await user.type(within(dialog).getByLabelText('Email'), BOB_AS_MEMBER.user.email);
    await user.click(within(dialog).getByRole('button', { name: 'Invite' }));

    expect(await within(dialog).findByText(/already a member/)).toBeInTheDocument();
  });

  it('changes a role optimistically, then settles on the server value', async () => {
    const user = userEvent.setup();
    renderWithProviders(routes, '/w/acme/members');
    await screen.findByText('Bob');

    const select = screen.getByRole('combobox', { name: 'Role for Bob' });
    await user.selectOptions(select, 'Owner');

    // Optimistic: reflects the chosen role immediately, before the delayed response resolves.
    expect(select).toHaveValue('Owner');
    await waitFor(() => expect(select).not.toBeDisabled());
  });

  it('rolls back and shows the server explanation when demoting the last owner', async () => {
    roleChangeStatus = 422;
    const user = userEvent.setup();
    renderWithProviders(routes, '/w/acme/members');
    await screen.findByText('Bob');

    const select = screen.getByRole('combobox', { name: 'Role for Dario' });
    expect(select).toHaveValue('Owner');
    await user.selectOptions(select, 'Member');

    expect(await screen.findByText('A workspace must keep at least one owner.')).toBeInTheDocument();
    await waitFor(() => expect(select).toHaveValue('Owner'));
  });

  it('confirms removal naming the member, then removes them', async () => {
    const user = userEvent.setup();
    renderWithProviders(routes, '/w/acme/members');
    await screen.findByText('Bob');

    const bobRow = screen.getByText('Bob').closest('tr')!;
    await user.click(within(bobRow).getByRole('button', { name: 'Remove' }));

    const dialog = screen.getByRole('dialog', { name: 'Remove member' });
    expect(dialog).toHaveTextContent('Remove Bob from Acme?');
    await user.click(within(dialog).getByRole('button', { name: 'Remove' }));

    await waitFor(() => expect(screen.queryByText('Bob')).not.toBeInTheDocument());
  });

  it('leaving names the workspace and sends the caller home', async () => {
    const user = userEvent.setup();
    const { router } = renderWithProviders(routes, '/w/acme/members');
    await screen.findByText('Bob');

    const ownRow = within(screen.getByRole('table')).getByText('Dario').closest('tr')!;
    await user.click(within(ownRow).getByRole('button', { name: 'Leave' }));

    const dialog = screen.getByRole('dialog', { name: 'Leave workspace' });
    expect(dialog).toHaveTextContent('Leave Acme?');
    await user.click(within(dialog).getByRole('button', { name: 'Leave' }));

    await waitFor(() => expect(router.state.location.pathname).toBe('/'));
  });
});

describe('as a plain member', () => {
  beforeEach(() => {
    currentWorkspace = workspace('Member');
    members = [DARIO_AS_MEMBER, BOB_AS_OWNER];
  });

  it('shows the list with no mutation controls except leaving', async () => {
    renderWithProviders(routes, '/w/acme/members');
    await screen.findByText('Bob');

    expect(screen.queryByRole('button', { name: 'Invite member' })).not.toBeInTheDocument();
    expect(screen.queryByRole('combobox')).not.toBeInTheDocument();
    const table = screen.getByRole('table');
    const bobRow = within(table).getByText('Bob').closest('tr')!;
    expect(within(bobRow).queryByRole('button')).not.toBeInTheDocument();
    const ownRow = within(table).getByText('Dario').closest('tr')!;
    expect(within(ownRow).getByRole('button', { name: 'Leave' })).toBeInTheDocument();
  });
});
