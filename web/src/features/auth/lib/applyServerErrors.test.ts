import type { UseFormSetError } from 'react-hook-form';
import { describe, expect, it, vi } from 'vitest';
import { ApiError } from '../../../shared/api/client';
import { applyServerErrors } from './applyServerErrors';

interface Values {
  email: string;
  password: string;
}

describe('applyServerErrors', () => {
  it('maps ProblemDetails.errors onto individual fields and returns no banner', () => {
    const setError = vi.fn() as unknown as UseFormSetError<Values>;
    const error = new ApiError({
      title: 'One or more validation errors occurred.',
      status: 400,
      errors: { email: ['Email is not a valid email address.'] },
    });

    const banner = applyServerErrors(error, setError);

    expect(setError).toHaveBeenCalledWith('email', {
      type: 'server',
      message: 'Email is not a valid email address.',
    });
    expect(banner).toBeNull();
  });

  it('returns the server detail as a banner when there are no field errors', () => {
    const setError = vi.fn() as unknown as UseFormSetError<Values>;
    const error = new ApiError({
      title: 'Authentication failed.',
      status: 401,
      detail: 'Invalid email or password.',
    });

    const banner = applyServerErrors(error, setError);

    expect(setError).not.toHaveBeenCalled();
    expect(banner).toBe('Invalid email or password.');
  });

  it('falls back to a generic message for a non-ApiError failure', () => {
    const setError = vi.fn() as unknown as UseFormSetError<Values>;

    const banner = applyServerErrors(new Error('network down'), setError);

    expect(banner).toBe('Something went wrong. Please try again.');
  });
});
