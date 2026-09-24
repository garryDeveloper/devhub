import { Link } from 'react-router-dom';

// Honest placeholder: password reset by email is post-MVP (auth-spec.md §8). Do not ship a form
// that silently does nothing (DEVHUB-020 tasks).
export function ForgotPasswordPage() {
  return (
    <div className="space-y-4 text-center">
      <h1 className="text-lg font-semibold text-slate-900">Forgot password</h1>
      <p className="text-sm text-slate-500">
        Password reset by email isn't available yet. This is coming in a future update.
      </p>
      <Link to="/login" className="text-sm font-medium text-blue-600 hover:underline">
        Back to login
      </Link>
    </div>
  );
}
