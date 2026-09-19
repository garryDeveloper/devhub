// Centralized, typed query keys. Feature tickets extend this object —
// never inline a string array as a query key.
export const qk = {
  health: ['health'] as const,
};
