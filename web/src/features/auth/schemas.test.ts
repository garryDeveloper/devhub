import { describe, expect, it } from 'vitest';
import { loginSchema, registerSchema } from './schemas';

describe('loginSchema', () => {
  it('requires an email and a password', () => {
    const result = loginSchema.safeParse({ email: '', password: '' });
    expect(result.success).toBe(false);
  });

  it('rejects a malformed email', () => {
    const result = loginSchema.safeParse({
      email: 'not-an-email',
      password: 'whatever',
    });
    expect(result.success).toBe(false);
  });

  it('accepts a valid email and non-empty password', () => {
    const result = loginSchema.safeParse({
      email: 'dario@example.com',
      password: 'anything',
    });
    expect(result.success).toBe(true);
  });
});

describe('registerSchema', () => {
  it('rejects a password shorter than 10 characters', () => {
    const result = registerSchema.safeParse({
      email: 'dario@example.com',
      password: 'short1',
      displayName: 'Dario',
    });
    expect(result.success).toBe(false);
  });

  it('trims the display name and rejects an empty one', () => {
    const result = registerSchema.safeParse({
      email: 'dario@example.com',
      password: 'a-long-enough-password',
      displayName: '   ',
    });
    expect(result.success).toBe(false);
  });

  it('accepts a valid registration payload', () => {
    const result = registerSchema.safeParse({
      email: 'dario@example.com',
      password: 'a-long-enough-password',
      displayName: 'Dario',
    });
    expect(result.success).toBe(true);
  });
});
