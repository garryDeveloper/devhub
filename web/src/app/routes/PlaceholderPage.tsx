import { useParams } from 'react-router-dom';

// Stands in for every screen that has its own ticket (issues, board, releases, ...).
// This shell owns navigation and layout only — not the data on these screens.
export function PlaceholderPage({ title }: { title: string }) {
  const params = useParams();
  const hasParams = Object.keys(params).length > 0;

  return (
    <div className="space-y-2">
      <h1 className="text-lg font-semibold text-slate-900">{title}</h1>
      <p className="text-sm text-slate-500">This screen is built in its own ticket.</p>
      {hasParams && (
        <pre className="mt-4 w-fit rounded-md bg-slate-100 px-3 py-2 text-xs text-slate-600">
          {JSON.stringify(params, null, 2)}
        </pre>
      )}
    </div>
  );
}
