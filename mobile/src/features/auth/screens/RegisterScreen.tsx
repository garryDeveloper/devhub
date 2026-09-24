import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm } from 'react-hook-form';

import { Button } from '../../../shared/components/Button';
import { AuthFormLayout } from '../components/AuthFormLayout';
import { FormField } from '../components/FormField';
import { useAuth } from '../hooks/useAuth';
import { applyServerErrors } from '../lib/applyServerErrors';
import type { RegisterFormValues } from '../schemas';
import { registerSchema } from '../schemas';

export function RegisterScreen() {
  const { register } = useAuth();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { email: '', password: '', displayName: '' },
  });

  const onSubmit = handleSubmit(async (values) => {
    setFormError(null);
    try {
      await register(values.email, values.password, values.displayName);
    } catch (error) {
      const banner = applyServerErrors(error, setError);
      if (banner) setFormError(banner);
    }
  });

  return (
    <AuthFormLayout title="Create an account" banner={formError}>
      <FormField
        control={control}
        name="displayName"
        label="Display name"
        error={errors.displayName?.message}
        autoComplete="name"
        textContentType="name"
        returnKeyType="next"
      />
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
        autoComplete="new-password"
        textContentType="newPassword"
        returnKeyType="go"
        onSubmitEditing={() => void onSubmit()}
      />
      <Button
        label="Create account"
        loading={isSubmitting}
        onPress={() => void onSubmit()}
      />
    </AuthFormLayout>
  );
}
