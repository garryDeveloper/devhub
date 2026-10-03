import { cn } from '../lib/cn';

export interface AvatarProps {
  name: string;
  imageUrl?: string | null;
  className?: string;
}

function initials(name: string): string {
  const parts = name.trim().split(/\s+/);
  const first = parts[0]?.[0] ?? '';
  const last = parts.length > 1 ? parts[parts.length - 1]?.[0] ?? '' : '';
  return (first + last).toUpperCase();
}

// No user has a real avatarUrl until EPIC 15 (UserDto comments) — initials are the only case
// this needs to render well today, but `imageUrl` is accepted now so nothing here changes later.
export function Avatar({ name, imageUrl, className }: AvatarProps) {
  if (imageUrl) {
    return (
      <img
        src={imageUrl}
        alt=""
        className={cn('h-8 w-8 rounded-full object-cover', className)}
      />
    );
  }

  return (
    <span
      aria-hidden="true"
      className={cn(
        'flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-slate-200 text-xs font-medium text-slate-700',
        className,
      )}
    >
      {initials(name)}
    </span>
  );
}
