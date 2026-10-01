// A mirror of WorkspaceSlug.FromName (DevHub.Domain), used only for the read-only address preview
// in the create modal. The server derives the real slug; if the two ever disagreed, the user would
// land on an address different from the one they were shown — slugify.test.ts uses the same cases
// as the backend's WorkspaceTests to keep them in step.

const MIN_LENGTH = 3;
const MAX_LENGTH = 50;
const KEBAB_CASE = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

/** "Café Élite — Team 2" → "cafe-elite-team-2". Null when nothing usable is left ("東京チーム"). */
export function slugify(name: string): string | null {
  // NFD splits "é" into "e" + a combining accent (Unicode category Mn); dropping the accents keeps
  // "cafe" instead of turning "Café" into "caf".
  let slug = name
    .normalize('NFD')
    .replace(/\p{Mn}/gu, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');

  // Truncating can cut right after a separator, so trim again.
  if (slug.length > MAX_LENGTH) {
    slug = slug.slice(0, MAX_LENGTH).replace(/-+$/, '');
  }

  return slug.length >= MIN_LENGTH && KEBAB_CASE.test(slug) ? slug : null;
}
