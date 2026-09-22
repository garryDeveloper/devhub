import { Outlet } from 'react-router-dom';

// Bare centered layout for /login, /register, /forgot-password. No sidebar,
// no auth check — RequireAuth guards land in DEVHUB-020.
export function AuthLayout() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-4">
      <div className="w-full max-w-sm space-y-6">
        <p className="text-center text-lg font-semibold tracking-wide text-slate-900">
          DEVHUB
        </p>
        <div className="rounded-lg border border-slate-200 bg-white p-6 shadow-sm">
          <Outlet />
        </div>
      </div>
    </div>
  );
}
