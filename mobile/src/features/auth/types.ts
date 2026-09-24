// Mirrors DevHub.Application.Users.Contracts.UserDto (api-endpoints.md §1). Same as web.
export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  avatarUrl: string | null;
}

// Mirrors DevHub.Application.Auth.Contracts.AuthResponse — the shape of both
// POST /api/auth/register (201) and POST /api/auth/login (200).
export interface AuthResponse {
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
  user: AuthUser;
}

// RefreshResponse (POST /api/auth/refresh) is mirrored in shared/api/client.ts, the only caller.
