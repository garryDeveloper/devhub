// Ported 1:1 from web/src/styles/index.css so both clients agree on meaning
// (docs/screens-and-navigation.md §8 "Shared UI rules"). Color is never the
// only signal — pair with a glyph or label.
export const colors = {
  background: '#ffffff',
  surface: '#f8fafc',
  border: '#e2e8f0',
  text: '#0f172a',
  textMuted: '#64748b',
  primary: '#0f172a',
  primaryText: '#ffffff',
  danger: '#dc2626',
  dangerSurface: '#fef2f2',

  status: {
    backlog: '#9ca3af',
    todo: '#94a3b8',
    inProgress: '#3b82f6',
    inReview: '#f59e0b',
    done: '#22c55e',
    canceled: '#f87171',
  },

  health: {
    healthy: '#22c55e',
    degraded: '#f59e0b',
    unhealthy: '#ef4444',
    unknown: '#9ca3af',
  },
} as const;
