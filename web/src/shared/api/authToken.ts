// Access token lives in memory only — never localStorage, never a cookie a script can read
// (frontend-web-architecture.md §7). A module-scoped variable is the "memory": it survives
// re-renders but is gone on a hard refresh, which is exactly the point.
let accessToken: string | null = null;

export function getAccessToken(): string | null {
  return accessToken;
}

export function setAccessToken(token: string | null): void {
  accessToken = token;
}
