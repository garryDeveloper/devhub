import { useContext } from 'react';
import { ToastContext } from '../lib/toast-context';
import type { ToastContextValue } from '../lib/toast-context';

export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used within a ToastProvider');
  return ctx;
}
