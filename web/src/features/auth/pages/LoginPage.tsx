import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { Button } from '../../../shared/components/Button';
import { useAuth } from '../hooks/useAuth';
import { applyServerErrors } from '../lib/applyServerErrors';
import type { LoginFormValues } from '../schemas';
import { loginSchema } from '../schemas';

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({ resolver: zodResolver(loginSchema) });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      await login(values.email, values.password);
      const returnTo = searchParams.get('returnTo');
      navigate(returnTo || '/', { replace: true });
    } catch (error) {
      // Do not reset the form here: the entered email must survive a failed attempt.
      const banner = applyServerErrors(error, setError);
      if (banner) setFormError(banner);
    }
  });

  return (
    <form className="space-y-4" onSubmit={onSubmit} noValidate>
      <h1 className="text-lg font-semibold text-slate-900">Log in</h1>

      {formError && (
        <p role="alert" className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">
          {formError}
        </p>
      )}

      <div className="space-y-1">
        <label htmlFor="email" className="text-sm font-medium text-slate-700">
          Email
        </label>
        <input
          id="email"
          type="email"
          autoComplete="email"
          className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-600"
          {...register('email')}
        />
        {errors.email && (
          <p className="text-sm text-red-600">{errors.email.message}</p>
        )}
      </div>

      <div className="space-y-1">
        <label htmlFor="password" className="text-sm font-medium text-slate-700">
          Password
        </label>
        <input
          id="password"
          type="password"
          autoComplete="current-password"
          className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-600"
          {...register('password')}
        />
        {errors.password && (
          <p className="text-sm text-red-600">{errors.password.message}</p>
        )}
      </div>

      <Button type="submit" className="w-full" disabled={isSubmitting}>
        {isSubmitting ? 'Logging in…' : 'Log in'}
      </Button>

      <div className="flex justify-between text-sm text-slate-500">
        <Link to="/register" className="font-medium text-blue-600 hover:underline">
          Create an account
        </Link>
        <Link to="/forgot-password" className="hover:underline">
          Forgot password?
        </Link>
      </div>
    </form>
  );
}
