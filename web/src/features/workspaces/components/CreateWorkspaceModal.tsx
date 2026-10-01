import { zodResolver } from '@hookform/resolvers/zod';
import { useId, useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { ApiError } from '../../../shared/api/client';
import { Button } from '../../../shared/components/Button';
import { Modal } from '../../../shared/components/Modal';
import { useCreateWorkspace } from '../hooks/useCreateWorkspace';
import { slugify } from '../lib/slugify';

// Mirrors CreateWorkspaceValidator: 1–80 characters after trimming, and a name the address can be
// derived from. The API stays the authority; this only saves a round-trip.
const schema = z.object({
  name: z
    .string()
    .trim()
    .min(1, 'Name is required.')
    .max(80, 'Name must be 80 characters or fewer.')
    .refine((name) => slugify(name) !== null, {
      message:
        'Use at least 3 letters or digits (a–z, 0–9) so the workspace gets an address.',
    }),
});

type FormValues = z.infer<typeof schema>;

export interface CreateWorkspaceModalProps {
  open: boolean;
  onClose: () => void;
}

export function CreateWorkspaceModal({
  open,
  onClose,
}: CreateWorkspaceModalProps) {
  // The form is a child that mounts only while the modal is open (Modal renders nothing when
  // closed), so every opening starts clean — no leftover name or error, and no reset effect.
  return (
    <Modal open={open} onClose={onClose} title="Create workspace">
      <CreateWorkspaceForm onClose={onClose} />
    </Modal>
  );
}

// The address is a read-only preview of the slug the server will derive (DEVHUB-027). Since the
// name is the only thing the user controls, every slug problem the server reports — taken (409)
// or invalid (400 errors.slug) — is shown on the name field: that is where it gets fixed.
function CreateWorkspaceForm({ onClose }: { onClose: () => void }) {
  const navigate = useNavigate();
  const createWorkspace = useCreateWorkspace();
  const [formError, setFormError] = useState<string | null>(null);
  const previewId = useId();

  const {
    register,
    handleSubmit,
    setError,
    control,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: '' },
  });

  // useWatch, not watch(): it subscribes only to this field and plays well with memoization.
  const slug = slugify(useWatch({ control, name: 'name' }) ?? '');

  const onSubmit = handleSubmit(async ({ name }) => {
    setFormError(null);
    try {
      const created = await createWorkspace.mutateAsync(name);
      onClose();
      // Straight into the new workspace (acceptance criterion). The cache already holds it, so
      // the workspace route resolves the slug without a "not found" flash.
      navigate(`/w/${created.slug}`);
    } catch (error) {
      if (!(error instanceof ApiError)) {
        setFormError('Something went wrong. Please try again.');
        return;
      }

      if (error.status === 409) {
        setError('name', {
          type: 'server',
          message: `The address /w/${slugify(name)} is already taken. Choose a different name.`,
        });
        return;
      }

      const fieldMessage = error.errors?.name?.[0] ?? error.errors?.slug?.[0];
      if (fieldMessage) {
        setError('name', { type: 'server', message: fieldMessage });
        return;
      }

      setFormError(error.detail ?? error.title);
    }
  });

  return (
    <form className="space-y-4" onSubmit={onSubmit} noValidate>
      {formError && (
        <p
          role="alert"
          className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700"
        >
          {formError}
        </p>
      )}

      <div className="space-y-1">
        <label
          htmlFor="workspace-name"
          className="text-sm font-medium text-slate-700"
        >
          Name
        </label>
        <input
          id="workspace-name"
          type="text"
          autoComplete="off"
          aria-describedby={previewId}
          aria-invalid={errors.name ? true : undefined}
          className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-600"
          {...register('name')}
        />
        <p id={previewId} aria-live="polite" className="text-sm text-slate-500">
          Address:{' '}
          {slug ? (
            <span className="font-mono text-slate-700">/w/{slug}</span>
          ) : (
            <span>—</span>
          )}
        </p>
        {errors.name && (
          <p className="text-sm text-red-600">{errors.name.message}</p>
        )}
      </div>

      <div className="flex justify-end gap-2">
        <Button variant="secondary" onClick={onClose}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Creating…' : 'Create workspace'}
        </Button>
      </div>
    </form>
  );
}
