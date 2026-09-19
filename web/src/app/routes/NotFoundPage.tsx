import { Link } from 'react-router-dom';

// Unknown route renders inside the app shell, not a blank page
// (docs/screens-and-navigation.md §2 "Rules").
export function NotFoundPage() {
  return (
    <div className="flex flex-col items-center gap-2 py-16 text-center">
      <p className="text-lg font-semibold text-slate-900">Page not found</p>
      <p className="text-sm text-slate-500">The page you're looking for doesn't exist.</p>
      <Link to="/" className="mt-2 text-sm font-medium text-blue-600 hover:underline">
        Back to overview
      </Link>
    </div>
  );
}
