import { beforeEach, describe, expect, it } from 'vitest';
import type { Workspace } from '../types';
import {
  getLastWorkspaceSlug,
  resolveHomeWorkspace,
  setLastWorkspaceSlug,
} from './lastWorkspace';

const workspace = (slug: string): Workspace => ({
  id: `id-${slug}`,
  name: slug,
  slug,
  role: 'Owner',
  memberCount: 1,
  createdAt: '2026-10-01T00:00:00Z',
});

describe('lastWorkspace', () => {
  beforeEach(() => localStorage.clear());

  it('remembers the last workspace per user', () => {
    setLastWorkspaceSlug('alice', 'acme');
    setLastWorkspaceSlug('bob', 'globex');

    expect(getLastWorkspaceSlug('alice')).toBe('acme');
    expect(getLastWorkspaceSlug('bob')).toBe('globex');
    expect(getLastWorkspaceSlug('carol')).toBeNull();
  });

  it('resolves home to the remembered workspace when the user still belongs to it', () => {
    const list = [workspace('acme'), workspace('globex')];

    expect(resolveHomeWorkspace(list, 'globex')?.slug).toBe('globex');
  });

  it('falls back to the first workspace for a stale or missing slug', () => {
    const list = [workspace('acme'), workspace('globex')];

    expect(resolveHomeWorkspace(list, 'left-long-ago')?.slug).toBe('acme');
    expect(resolveHomeWorkspace(list, null)?.slug).toBe('acme');
  });

  it('resolves to null when the user has no workspaces', () => {
    expect(resolveHomeWorkspace([], 'acme')).toBeNull();
  });
});
