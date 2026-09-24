import type { FieldValues, Path, UseFormSetError } from 'react-hook-form';
import { ApiError } from '../../../shared/api/client';

// ProblemDetails.errors (validation, e.g. register) maps onto individual fields; anything else
// (401 invalid credentials, 409 email taken, 429 locked out) has no field to attach to and
// becomes a form-level banner instead. Returns the banner message, or null when field errors
// were applied and no banner is needed.
export function applyServerErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
): string | null {
  if (!(error instanceof ApiError)) {
    return 'Something went wrong. Please try again.';
  }

  if (error.errors) {
    for (const [field, messages] of Object.entries(error.errors)) {
      setError(field as Path<T>, { type: 'server', message: messages[0] });
    }
    return null;
  }

  return error.detail ?? error.title;
}
