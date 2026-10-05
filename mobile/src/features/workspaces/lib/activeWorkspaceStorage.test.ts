import AsyncStorage from '@react-native-async-storage/async-storage';

import {
  getStoredWorkspaceId,
  resolveActiveWorkspace,
  setStoredWorkspaceId,
} from './activeWorkspaceStorage';
import type { Workspace } from '../types';

const WORKSPACES: Workspace[] = [
  {
    id: 'ws-1',
    name: 'Acme',
    slug: 'acme',
    role: 'Owner',
    memberCount: 1,
    createdAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 'ws-2',
    name: 'Beta Co',
    slug: 'beta-co',
    role: 'Member',
    memberCount: 1,
    createdAt: '2026-01-02T00:00:00Z',
  },
];

describe('activeWorkspaceStorage', () => {
  beforeEach(async () => {
    await AsyncStorage.clear();
  });

  it('round-trips a stored workspace id per user', async () => {
    await setStoredWorkspaceId('user-1', 'ws-2');

    expect(await getStoredWorkspaceId('user-1')).toBe('ws-2');
    expect(await getStoredWorkspaceId('user-2')).toBeNull();
  });

  it('resolveActiveWorkspace prefers the stored id when it is still a member workspace', () => {
    expect(resolveActiveWorkspace(WORKSPACES, 'ws-2')).toEqual(WORKSPACES[1]);
  });

  it('resolveActiveWorkspace falls back to the first workspace when the stored id is unknown', () => {
    expect(resolveActiveWorkspace(WORKSPACES, 'ws-gone')).toEqual(
      WORKSPACES[0],
    );
    expect(resolveActiveWorkspace(WORKSPACES, null)).toEqual(WORKSPACES[0]);
  });

  it('resolveActiveWorkspace returns null when the user has no workspace', () => {
    expect(resolveActiveWorkspace([], 'ws-1')).toBeNull();
  });
});
