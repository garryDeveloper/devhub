import { isRouteErrorResponse, useRouteError } from 'react-router-dom';
import { ErrorState } from '../../shared/components/ErrorState';

// React Router's errorElement wraps each route's render, so a thrown error
// degrades that one screen instead of producing a white screen
// (docs/tech-specs/frontend-web-architecture.md §6).
export function RouteErrorBoundary() {
  const error = useRouteError();

  const message = isRouteErrorResponse(error)
    ? (error.data?.message ?? error.statusText)
    : error instanceof Error
      ? error.message
      : 'An unexpected error occurred.';

  return (
    <div className="p-6">
      <ErrorState
        title="This screen crashed"
        message={message}
        onRetry={() => window.location.reload()}
      />
    </div>
  );
}
