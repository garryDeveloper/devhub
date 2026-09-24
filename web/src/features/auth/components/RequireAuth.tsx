import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

// Guards the private route tree. AuthProvider has already finished bootstrapping by the time
// this renders (it gates the whole app while pending), so the only question here is whether a
// session exists. This is a UX redirect, not a security boundary — the API is the real authority
// (auth-spec.md §5).
export function RequireAuth() {
  const { user, sessionExpired } = useAuth();
  const location = useLocation();

  if (!user) {
    const params = new URLSearchParams();
    // expired=1 lets LoginPage explain why the user is here (DEVHUB-021).
    if (sessionExpired) params.set('expired', '1');
    params.set('returnTo', `${location.pathname}${location.search}`);
    return <Navigate to={`/login?${params.toString()}`} replace />;
  }

  return <Outlet />;
}
