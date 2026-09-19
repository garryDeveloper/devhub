import { useState } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { Drawer } from '../../shared/components/Drawer';
import { cn } from '../../shared/lib/cn';

interface NavItem {
  to: string;
  label: string;
  icon: string;
}

// A fixed demo workspace/project until DEVHUB-020 (auth) and the workspace
// feature exist to supply a real one. The shell does not fetch this.
const primaryNav: NavItem[] = [
  { to: '/', label: 'Overview', icon: '⌂' },
  { to: '/w/demo/projects', label: 'Projects', icon: '▣' },
  { to: '/debug/health', label: 'API health', icon: '⚕' },
];

const secondaryNav: NavItem[] = [
  { to: '/notifications', label: 'Notifications', icon: '🔔' },
  { to: '/account/settings', label: 'Settings', icon: '⚙' },
  { to: '/account/profile', label: 'Profile', icon: '👤' },
];

function navLinkClassName({ isActive }: { isActive: boolean }) {
  return cn(
    'flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium',
    'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600',
    isActive ? 'bg-slate-900 text-white' : 'text-slate-700 hover:bg-slate-100',
  );
}

function NavLinks({ onNavigate }: { onNavigate?: () => void }) {
  return (
    <nav className="flex flex-1 flex-col justify-between">
      <ul className="space-y-1">
        {primaryNav.map((item) => (
          <li key={item.to}>
            <NavLink to={item.to} end={item.to === '/'} onClick={onNavigate} className={navLinkClassName}>
              <span aria-hidden="true">{item.icon}</span>
              {item.label}
            </NavLink>
          </li>
        ))}
      </ul>
      <ul className="space-y-1 border-t border-slate-200 pt-2">
        {secondaryNav.map((item) => (
          <li key={item.to}>
            <NavLink to={item.to} onClick={onNavigate} className={navLinkClassName}>
              <span aria-hidden="true">{item.icon}</span>
              {item.label}
            </NavLink>
          </li>
        ))}
      </ul>
    </nav>
  );
}

// Sidebar + top bar + <Outlet/>. Persists across child navigation so the
// sidebar stays mounted (docs/tickets DEVHUB-008 acceptance criteria).
export function AppShell() {
  const [mobileNavOpen, setMobileNavOpen] = useState(false);

  return (
    <div className="flex h-screen flex-col bg-white">
      <header className="flex h-14 shrink-0 items-center gap-3 border-b border-slate-200 px-4">
        <button
          type="button"
          onClick={() => setMobileNavOpen(true)}
          aria-label="Open navigation"
          className="rounded p-2 text-slate-600 hover:bg-slate-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-600 md:hidden"
        >
          ☰
        </button>
        <span className="text-sm font-semibold tracking-wide text-slate-900">DEVHUB</span>
        <div className="ml-auto flex items-center gap-2 text-sm text-slate-400">
          <span className="hidden sm:inline">Search</span>
          <kbd className="rounded border border-slate-300 px-1.5 py-0.5 text-xs">⌘K</kbd>
        </div>
      </header>

      <div className="flex min-h-0 flex-1">
        <aside className="hidden w-60 shrink-0 border-r border-slate-200 p-4 md:flex">
          <NavLinks />
        </aside>

        <main className="min-w-0 flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>

      <Drawer open={mobileNavOpen} onClose={() => setMobileNavOpen(false)} title="Menu" side="left">
        <NavLinks onNavigate={() => setMobileNavOpen(false)} />
      </Drawer>
    </div>
  );
}
