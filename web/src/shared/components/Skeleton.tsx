import type { HTMLAttributes } from 'react';
import { cn } from '../lib/cn';

// A skeleton is a building block: compose it to match the final layout
// (e.g. a row of skeletons shaped like a table row), not a generic spinner.
export function Skeleton({
  className,
  ...props
}: HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      role="presentation"
      aria-hidden="true"
      className={cn('animate-pulse rounded-md bg-slate-200', className)}
      {...props}
    />
  );
}
