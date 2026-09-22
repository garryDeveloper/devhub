import { Button } from './Button';

export interface ErrorStateProps {
  title?: string;
  message: string;
  onRetry?: () => void;
}

// Message + Retry — every failed data view uses this, not a bespoke one
// (docs/tech-specs/frontend-web-architecture.md §6).
export function ErrorState({
  title = 'Something went wrong',
  message,
  onRetry,
}: ErrorStateProps) {
  return (
    <div
      role="alert"
      className="flex flex-col items-center gap-2 rounded-lg border border-red-200 bg-red-50 px-6 py-12 text-center"
    >
      <p className="text-sm font-medium text-red-900">{title}</p>
      <p className="text-sm text-red-700">{message}</p>
      {onRetry && (
        <Button
          variant="secondary"
          size="sm"
          className="mt-2"
          onClick={onRetry}
        >
          Retry
        </Button>
      )}
    </div>
  );
}
