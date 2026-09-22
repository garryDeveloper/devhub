import type { MouseEvent, ReactNode } from 'react';
import { useId } from 'react';
import { createPortal } from 'react-dom';
import { useFocusTrap } from '../hooks/useFocusTrap';
import { cn } from '../lib/cn';

export interface DrawerProps {
  open: boolean;
  onClose: () => void;
  title: string;
  children: ReactNode;
  side?: 'left' | 'right';
}

// Used for the issue detail overlay (docs/screens-and-navigation.md §2) and,
// in this shell, for the mobile nav panel below the md breakpoint.
export function Drawer({
  open,
  onClose,
  title,
  children,
  side = 'right',
}: DrawerProps) {
  const titleId = useId();
  const containerRef = useFocusTrap(open, onClose);

  if (!open) return null;

  function handleBackdropClick(e: MouseEvent<HTMLDivElement>) {
    if (e.target === e.currentTarget) onClose();
  }

  return createPortal(
    <div
      className="fixed inset-0 z-50 flex bg-slate-900/40"
      onClick={handleBackdropClick}
    >
      <div
        ref={containerRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
        className={cn(
          'h-full w-full max-w-sm bg-white p-6 shadow-xl focus:outline-none',
          side === 'right' ? 'ml-auto' : 'mr-auto',
        )}
      >
        <div className="mb-4 flex items-center justify-between">
          <h2 id={titleId} className="text-base font-semibold text-slate-900">
            {title}
          </h2>
          <button
            type="button"
            onClick={onClose}
            aria-label="Close"
            className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600 focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-600"
          >
            ✕
          </button>
        </div>
        {children}
      </div>
    </div>,
    document.body,
  );
}
