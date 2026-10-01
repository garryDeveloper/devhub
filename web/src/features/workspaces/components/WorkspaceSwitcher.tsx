import { useEffect, useId, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { Skeleton } from '../../../shared/components/Skeleton';
import { cn } from '../../../shared/lib/cn';
import { useWorkspaces } from '../hooks/useWorkspaces';
import type { Workspace } from '../types';

export interface WorkspaceSwitcherProps {
  /** The workspace the user is in, resolved by the shell from the URL (or the last one used). */
  current: Workspace | null;
  /** Called after a choice is made, e.g. to close the mobile drawer. */
  onNavigate?: () => void;
  /**
   * Opens the create modal. The shell owns the modal, not the switcher: inside the mobile drawer
   * the switcher unmounts when the drawer closes, and two focus traps would fight over focus.
   */
  onCreateWorkspace: () => void;
}

// A disclosure (button + list of links), not an ARIA menu: role="menu" promises arrow-key
// navigation, while plain links already work with Tab and Enter. Escape and a click outside close
// it. Switching is a client-side <Link>, so no page reload; no cache invalidation either — every
// workspace-scoped query key contains the workspace id (shared/api/queryKeys.ts).
export function WorkspaceSwitcher({
  current,
  onNavigate,
  onCreateWorkspace,
}: WorkspaceSwitcherProps) {
  const [open, setOpen] = useState(false);
  const panelId = useId();
  const containerRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const workspaces = useWorkspaces();

  useEffect(() => {
    if (!open) return;

    function handlePointerDown(event: PointerEvent) {
      if (!containerRef.current?.contains(event.target as Node)) setOpen(false);
    }
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setOpen(false);
        buttonRef.current?.focus();
      }
    }

    document.addEventListener('pointerdown', handlePointerDown);
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('pointerdown', handlePointerDown);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [open]);

  function choose() {
    setOpen(false);
    onNavigate?.();
  }

  function openCreate() {
    setOpen(false);
    onCreateWorkspace();
  }

  return (
    <div ref={containerRef} className="relative">
      <button
        ref={buttonRef}
        type="button"
        aria-expanded={open}
        aria-controls={panelId}
        // An explicit label: a visually hidden "Workspace: " span would be concatenated with the
        // name without a space ("Workspace:Acme") by some screen readers.
        aria-label={`Workspace: ${current?.name ?? 'No workspace'}`}
        onClick={() => setOpen((value) => !value)}
        className="flex w-full items-center justify-between gap-2 rounded-md border border-slate-200 px-3 py-2 text-left text-sm font-medium text-slate-900 hover:bg-slate-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-600"
      >
        <span className="truncate">{current?.name ?? 'No workspace'}</span>
        <span aria-hidden="true" className="text-slate-400">
          ▾
        </span>
      </button>

      {open && (
        <div
          id={panelId}
          className="absolute left-0 right-0 z-40 mt-1 rounded-md border border-slate-200 bg-white p-1 shadow-lg"
        >
          {workspaces.isPending && (
            <div className="space-y-1 p-2" aria-label="Loading workspaces">
              <Skeleton className="h-6" />
              <Skeleton className="h-6" />
            </div>
          )}

          {workspaces.isError && (
            <div role="alert" className="space-y-1 p-2 text-sm text-red-700">
              <p>Could not load your workspaces.</p>
              <button
                type="button"
                onClick={() => void workspaces.refetch()}
                className="font-medium underline focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-600"
              >
                Retry
              </button>
            </div>
          )}

          {workspaces.isSuccess && workspaces.data.length > 0 && (
            <ul
              aria-label="Your workspaces"
              className="max-h-72 overflow-y-auto"
            >
              {workspaces.data.map((workspace) => {
                const isCurrent = workspace.id === current?.id;
                return (
                  <li key={workspace.id}>
                    <Link
                      to={`/w/${workspace.slug}`}
                      onClick={choose}
                      aria-current={isCurrent ? 'page' : undefined}
                      className={cn(
                        'flex items-center justify-between gap-2 rounded px-2 py-1.5 text-sm',
                        'focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-600',
                        isCurrent
                          ? 'bg-slate-100 font-medium text-slate-900'
                          : 'text-slate-700 hover:bg-slate-50',
                      )}
                    >
                      <span className="truncate">
                        {isCurrent && <span aria-hidden="true">✓ </span>}
                        {workspace.name}
                      </span>
                      {/* Text, not just a color: the role must be readable on its own. */}
                      <span className="shrink-0 text-xs text-slate-500">
                        {workspace.role}
                      </span>
                    </Link>
                  </li>
                );
              })}
            </ul>
          )}

          <div
            className={cn(
              workspaces.isSuccess &&
                workspaces.data.length > 0 &&
                'mt-1 border-t border-slate-200 pt-1',
            )}
          >
            <button
              type="button"
              onClick={openCreate}
              className="w-full rounded px-2 py-1.5 text-left text-sm font-medium text-blue-700 hover:bg-slate-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-600"
            >
              + Create workspace
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
