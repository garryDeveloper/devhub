import { z } from 'zod';

// Copied from web/src/features/auth/schemas.ts on purpose — keep the two in sync.
// Client-side hints only — the API is the authority on password strength, the common-password
// deny-list and email uniqueness (auth-spec.md §2). Minimums here just mirror RegisterValidator
// so a user sees the same rule locally before round-tripping to the server.
export const loginSchema = z.object({
  email: z
    .string()
    .min(1, 'Email is required.')
    .email('Enter a valid email address.'),
  password: z.string().min(1, 'Password is required.'),
});

export type LoginFormValues = z.infer<typeof loginSchema>;

export const registerSchema = z.object({
  email: z
    .string()
    .min(1, 'Email is required.')
    .email('Enter a valid email address.'),
  password: z.string().min(10, 'Password must be at least 10 characters.'),
  displayName: z
    .string()
    .trim()
    .min(1, 'Display name is required.')
    .max(100, 'Display name must be 100 characters or fewer.'),
});

export type RegisterFormValues = z.infer<typeof registerSchema>;
