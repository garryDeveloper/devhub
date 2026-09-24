import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';

import { Button } from '../../../shared/components/Button';
import { AuthFormLayout } from '../components/AuthFormLayout';
import { FormField } from '../components/FormField';
import { useAuth } from '../hooks/useAuth';
import { applyServerErrors } from '../lib/applyServerErrors';
import type { LoginFormValues } from '../schemas';
import { loginSchema } from '../schemas';

// No navigate() on success: login() sets `user`, RootNavigator swaps AuthStack for AppTabs, and
// a deep link that arrived while signed out is replayed there (see RootNavigator).
export function LoginScreen() {
  const { login } = useAuth();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      await login(values.email, values.password);
    } catch (error) {
      // Do not reset the form here: the entered email must survive a failed attempt.
      const banner = applyServerErrors(error, setError);
      if (banner) setFormError(banner);
    }
  });

  return (
    <AuthFormLayout title="Log in" banner={formError}>
      <FormField
        control={control}
        name="email"
        label="Email"
        error={errors.email?.message}
        autoCapitalize="none"
        autoComplete="email"
        keyboardType="email-address"
        textContentType="emailAddress"
        returnKeyType="next"
      />
      <FormField
        control={control}
        name="password"
        label="Password"
        error={errors.password?.message}
        secureTextEntry
        autoCapitalize="none"
        autoComplete="current-password"
        textContentType="password"
        returnKeyType="go"
        onSubmitEditing={() => void onSubmit()}
      />
      <Button
        label="Log in"
        loading={isSubmitting}
        onPress={() => void onSubmit()}
      />
    </AuthFormLayout>
  );
}
