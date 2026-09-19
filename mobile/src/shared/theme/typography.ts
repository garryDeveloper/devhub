// No fixed line heights on body text — let it scale with the system font
// size (mobile-architecture.md §6).
export const typography = {
  title: { fontSize: 20, fontWeight: '600' },
  body: { fontSize: 15, fontWeight: '400' },
  bodyStrong: { fontSize: 15, fontWeight: '600' },
  caption: { fontSize: 13, fontWeight: '400' },
} as const;
