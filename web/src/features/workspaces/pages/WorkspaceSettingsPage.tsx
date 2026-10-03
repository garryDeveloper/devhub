import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useId, useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { applyServerErrors } from '../../auth/lib/applyServerErrors';
import { useCurrentWorkspace } from '../hooks/useCurrentWorkspace';
import { useUpdateWorkspace } from '../hooks/useUpdateWorkspace';
import { Button } from '../../../shared/components/Button';

const schema = z.object({
  name: z
    .string()
    .trim()
    .min(1, 'Name is required.')
    .max(80, 'Name must be 80 characters or fewer.'),
});

type FormValues = z.infer<typeof schema>;

// /w/:workspaceSlug/settings (screens-and-navigation.md §2). WorkspaceLayout has already
// confirmed the slug belongs to the caller, so `workspace` below is never null here.
export function WorkspaceSettingsPage() {
  const workspace = useCurrentWorkspace(null);
  if (!workspace) {
    return null;
  }

  const isOwner = workspace.role === 'Owner';

  return (
    <div className="max-w-xl space-y-6">
      <h1 className="text-lg font-semibold text-slate-900">Workspace settings</h1>

      {isOwner ? (
        <RenameForm workspaceId={workspace.id} initialName={workspace.name} slug={workspace.slug} />
      ) : (
        <ReadOnlyDetails name={workspace.name} slug={workspace.slug} />
      )}
    </div>
  );
}

function SlugField({ slug }: { slug: string }) {
  return (
    <div className="space-y-1">
      <span className="text-sm font-medium text-slate-700">Address</span>
      <p className="font-mono text-sm text-slate-900">/w/{slug}</p>
      <p className="text-sm text-slate-500">
        The address is set when the workspace is created and never changes, so links into it
        never break.
      </p>
    </div>
  );
}

// A member is never shown the rename form at all, even disabled — a disabled control a member
// cannot explain is worse than one that is simply not there (DEVHUB-028 technical notes).
function ReadOnlyDetails({ name, slug }: { name: string; slug: string }) {
  return (
    <div className="space-y-4 rounded-lg border border-slate-200 p-4">
      <div className="space-y-1">
        <span className="text-sm font-medium text-slate-700">Name</span>
        <p className="text-sm text-slate-900">{name}</p>
      </div>
      <SlugField slug={slug} />
      <p className="text-sm text-slate-500">Only a workspace owner can rename this workspace.</p>
    </div>
  );
}

function RenameForm({
  workspaceId,
  initialName,
  slug,
}: {
  workspaceId: string;
  initialName: string;
  slug: string;
}) {
  const updateWorkspace = useUpdateWorkspace(workspaceId);
  const [formError, setFormError] = useState<string | null>(null);
  const [savedName, setSavedName] = useState<string | null>(null);
  const nameId = useId();

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: initialName },
  });

  // The workspace list can change the name under this form (another tab, another owner) —
  // re-sync the untouched field rather than fighting the user's own edit.
  useEffect(() => {
    if (!isDirty) {
      reset({ name: initialName });
    }
  }, [initialName, isDirty, reset]);

  const onSubmit = handleSubmit(async ({ name }) => {
    setFormError(null);
    setSavedName(null);
    try {
      const updated = await updateWorkspace.mutateAsync(name);
      reset({ name: updated.name });
      setSavedName(updated.name);
    } catch (error) {
      setFormError(applyServerErrors(error, setError));
    }
  });

  return (
    <form className="space-y-4 rounded-lg border border-slate-200 p-4" onSubmit={onSubmit} noValidate>
      {formError && (
        <p role="alert" className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">
          {formError}
        </p>
      )}
      {savedName && !formError && (
        <p role="status" className="rounded-md bg-green-50 px-3 py-2 text-sm text-green-700">
          Saved.
        </p>
      )}

      <div className="space-y-1">
        <label htmlFor={nameId} className="text-sm font-medium text-slate-700">
          Name
        </label>
        <input
          id={nameId}
          type="text"
          aria-invalid={errors.name ? true : undefined}
          disabled={isSubmitting}
          className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm disabled:bg-slate-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-600"
          {...register('name')}
        />
        {errors.name && <p className="text-sm text-red-600">{errors.name.message}</p>}
      </div>

      <SlugField slug={slug} />

      <div className="flex justify-end">
        <Button type="submit" disabled={isSubmitting || !isDirty}>
          {isSubmitting ? 'Saving…' : 'Save'}
        </Button>
      </div>
    </form>
  );
}
