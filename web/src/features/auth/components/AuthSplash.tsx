// Rendered by AuthProvider while it is bootstrapping (reading the stored refresh token,
// refreshing, then GET /api/me), so nothing behind it — protected route or login screen — ever
// flashes before we know whether there is a session (DEVHUB-020 acceptance criteria).
export function AuthSplash() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50">
      <p className="animate-pulse text-sm font-semibold tracking-wide text-slate-400">
        DEVHUB
      </p>
    </div>
  );
}
