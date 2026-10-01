import { describe, expect, it } from 'vitest';
import { slugify } from './slugify';

// The same cases as the backend's WorkspaceTests (DEVHUB-023/024): the preview must show exactly
// the address the server will derive.
describe('slugify', () => {
  it.each([
    ['Acme', 'acme'],
    ['My Team', 'my-team'],
    ['  DevHub -- Platform  ', 'devhub-platform'],
    ['Café Élite', 'cafe-elite'],
    ['Team #2 (Backend)', 'team-2-backend'],
  ])('derives %j as %j', (name, expected) => {
    expect(slugify(name)).toBe(expected);
  });

  it('truncates to 50 characters without a trailing hyphen', () => {
    // 49 letters, a space, then more: the cut at 50 lands right on the hyphen.
    const name = `${'a'.repeat(49)} bcdef`;

    const slug = slugify(name);

    expect(slug).toBe('a'.repeat(49));
  });

  it.each(['AB', '🚀🚀🚀', '東京チーム', '   ', ''])(
    'returns null when %j leaves nothing usable',
    (name) => {
      expect(slugify(name)).toBeNull();
    },
  );
});
