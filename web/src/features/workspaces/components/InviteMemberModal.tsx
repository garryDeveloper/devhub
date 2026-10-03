import { zodResolver } from '@hookform/resolvers/zod';
import { useId, useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { ApiError } from '../../../shared/api/client';
import { Button } from '../../../shared/components/Button';
import { Modal } from '../../../shared/components/Modal';
import { useAddMember } from '../hooks/useAddMember';

const schema = z.object({
  email: z
    .string()
    .trim()
    .min(1, 'Email is required.')
    .max(254, 'Email must be 254 characters or fewer.')
    .email('Email is not a valid email address.'),
  role: z.enum(['Owner', 'Member']),
});

type FormValues = z.infer<typeof schema>;

export interface InviteMemberModalProps {
  workspaceId: string;
  open: boolean;
  onClose: () => void;
}

export function InviteMemberModal({ workspaceId, open, onClose }: InviteMemberModalProps) {
  return (
    <Modal open={open} onClose={onClose} title="Invite a member">
      <InviteMemberForm workspaceId={workspaceId} onClose={onClose} />
    </Modal>
  );
}

// Mounted only while the modal is open (Modal renders nothing when closed), so every opening
// starts clean — same trick as CreateWorkspaceForm (DEVHUB-027).
function InviteMemberForm({ workspaceId, onClose }: { workspaceId: string; onClose: () => void }) {
  const addMember = useAddMember(workspaceId);
  const [formError, setFormError] = useState<string | null>(null);
  const emailId = useId();
  const roleId = useId();

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', role: 'Member' },
  });

  const onSubmit = handleSubmit(async ({ email, role }) => {
    setFormError(null);
    try {
      await addMember.mutateAsync({ email, role });
      onClose();
    } catch (error) {
      if (!(error instanceof ApiError)) {
        setFormError('Something went wrong. Please try again.');
        return;
      }

      // Both answers are about the email the owner typed, so both land on that field — the
      // server's own wording ("No DevHub account with that email.") for the 404 case, exactly as
      // the ticket asks for.
      if (error.status === 404) {
        setError('email', { type: 'server', message: error.detail ?? 'No DevHub account with that email.' });
        return;
      }

      if (error.status === 409) {
        setError('email', { type: 'server', message: 'This person is already a member of the workspace.' });
        return;
      }

      const fieldMessage = error.errors?.email?.[0] ?? error.errors?.role?.[0];
      if (fieldMessage) {
        setError('email', { type: 'server', message: fieldMessage });
        return;
      }

      setFormError(error.detail ?? error.title);
    }
  });

  return (
    <form className="space-y-4" onSubmit={onSubmit} noValidate>
      {formError && (
        <p role="alert" className="rounded-md bg-red-50 px-3 py-2 text-sm text-red-700">
          {formError}
        </p>
      )}

      <div className="space-y-1">
        <label htmlFor={emailId} className="text-sm font-medium text-slate-700">
          Email
        </label>
        <input
          id={emailId}
          type="email"
          autoComplete="off"
          aria-invalid={errors.email ? true : undefined}
          className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-600"
          {...register('email')}
        />
        {errors.email && <p className="text-sm text-red-600">{errors.email.message}</p>}
      </div>

      <div className="space-y-1">
        <label htmlFor={roleId} className="text-sm font-medium text-slate-700">
          Role
        </label>
        <select
          id={roleId}
          className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-600"
          {...register('role')}
        >
          <option value="Member">Member</option>
          <option value="Owner">Owner</option>
        </select>
      </div>

      <div className="flex justify-end gap-2">
        <Button variant="secondary" onClick={onClose} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Inviting…' : 'Invite'}
        </Button>
      </div>
    </form>
  );
}
