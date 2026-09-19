import { useQuery } from '@tanstack/react-query';
import { apiFetch } from '../../shared/api/client';
import { qk } from '../../shared/api/queryKeys';
import { Skeleton } from '../../shared/components/Skeleton';
import { ErrorState } from '../../shared/components/ErrorState';

interface HealthResponse {
  status: string;
}

// The one data fetch this shell owns (task: "verify a call to /health renders
// in a debug page"). Every other route is a dumb placeholder.
export function HealthCheckPage() {
  const { data, error, isPending, refetch } = useQuery({
    queryKey: qk.health,
    queryFn: () => apiFetch<HealthResponse>('/health'),
  });

  return (
    <div className="space-y-4">
      <h1 className="text-lg font-semibold text-slate-900">API health</h1>
      <p className="text-sm text-slate-500">
        Confirms <code>VITE_API_URL</code> ({import.meta.env.VITE_API_URL}) and the API client are
        wired up.
      </p>

      {isPending && <Skeleton className="h-10 w-48" />}

      {error && (
        <ErrorState
          message={error instanceof Error ? error.message : 'Request failed'}
          onRetry={() => refetch()}
        />
      )}

      {data && (
        <pre className="w-fit rounded-md bg-slate-100 px-3 py-2 text-xs text-slate-600">
          {JSON.stringify(data, null, 2)}
        </pre>
      )}
    </div>
  );
}
