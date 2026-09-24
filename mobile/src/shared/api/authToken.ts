// Access token lives in memory only (mobile-architecture.md §4). A module-scoped variable is the
// "memory": it survives re-renders but is gone when the process dies, which is the point — the
// refresh token in SecureStore is what restores the session on the next cold start.
let accessToken: string | null = null;

export function getAccessToken(): string | null {
  return accessToken;
}

export function setAccessToken(token: string | null): void {
  accessToken = token;
}
