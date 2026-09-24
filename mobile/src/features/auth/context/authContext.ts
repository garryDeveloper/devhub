import { createContext } from 'react';

import type { AuthUser } from '../types';

// Same API as web (DEVHUB-020/021) so both clients read the same way.
export interface AuthContextValue {
  user: AuthUser | null;
  isBootstrapping: boolean;
  /** True after a refresh was rejected; reset by login, register and logout. */
  sessionExpired: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (
    email: string,
    password: string,
    displayName: string,
  ) => Promise<void>;
  logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
