import type { ReactNode } from 'react';
import { useCallback, useRef, useState } from 'react';
import type { ToastVariant } from '../lib/toast-context';
import { ToastContext } from '../lib/toast-context';

interface ToastItem {
  id: number;
  message: string;
  variant: ToastVariant;
}

const AUTO_DISMISS_MS = 4000;

const variantClasses: Record<ToastVariant, string> = {
  info: 'bg-slate-900 text-white',
  success: 'bg-green-600 text-white',
  error: 'bg-red-600 text-white',
};

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const nextId = useRef(0);

  const show = useCallback(
    (message: string, variant: ToastVariant = 'info') => {
      const id = nextId.current++;
      setToasts((prev) => [...prev, { id, message, variant }]);
      setTimeout(() => {
        setToasts((prev) => prev.filter((t) => t.id !== id));
      }, AUTO_DISMISS_MS);
    },
    [],
  );

  return (
    <ToastContext.Provider value={{ show }}>
      {children}
      {/* Live region so screen readers announce toasts (accessibility baseline §9). */}
      <div
        aria-live="polite"
        role="status"
        className="pointer-events-none fixed bottom-4 right-4 z-50 flex flex-col gap-2"
      >
        {toasts.map((t) => (
          <div
            key={t.id}
            className={`pointer-events-auto rounded-md px-4 py-2 text-sm shadow-lg ${variantClasses[t.variant]}`}
          >
            {t.message}
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}
