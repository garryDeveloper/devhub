// Refresh token storage trade-off (DEVHUB-020 technical notes, auth-spec.md §4):
//
// The spec's preferred approach is an httpOnly cookie set by the API, which a script — including
// an XSS payload — cannot read. The API does not implement that yet: POST /api/auth/register,
// /login and /refresh return `refreshToken` as a plain JSON field, so there is nothing for the
// browser to store except in JS-reachable storage. We use localStorage rather than sessionStorage
// so a closed tab does not silently log the user out.
//
// The accepted risk: an XSS vulnerability anywhere on this origin can read the refresh token and
// mint new sessions until the family is revoked. This is mitigated, not eliminated, by: the
// 15-minute access token lifetime, single-use rotation with family-wide reuse revocation
// (auth-spec.md §4), and this project's low blast radius (a portfolio/learning app, not a
// production service with real user data). The correct long-term fix is an httpOnly refresh
// cookie issued by the API — that is API work, not something the client can opt into, so it is
// left as a follow-up rather than solved here.
const STORAGE_KEY = 'devhub.refreshToken';

export function getStoredRefreshToken(): string | null {
  return localStorage.getItem(STORAGE_KEY);
}

export function setStoredRefreshToken(token: string): void {
  localStorage.setItem(STORAGE_KEY, token);
}

export function clearStoredRefreshToken(): void {
  localStorage.removeItem(STORAGE_KEY);
}
