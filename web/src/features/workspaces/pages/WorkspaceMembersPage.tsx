import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../auth/hooks/useAuth';
import { Button } from '../../../shared/components/Button';
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog';
import { EmptyState } from '../../../shared/components/EmptyState';
import { ErrorState } from '../../../shared/components/ErrorState';
import { Skeleton } from '../../../shared/components/Skeleton';
import { Avatar } from '../../../shared/components/Avatar';
import { InviteMemberModal } from '../components/InviteMemberModal';
import { useChangeMemberRole } from '../hooks/useChangeMemberRole';
import { useCurrentWorkspace } from '../hooks/useCurrentWorkspace';
import { useMembers } from '../hooks/useMembers';
import { useRemoveMember } from '../hooks/useRemoveMember';
import type { Member, WorkspaceRole } from '../types';

function formatJoinedDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

// /w/:workspaceSlug/members (screens-and-navigation.md §2). WorkspaceLayout has already
// confirmed the slug belongs to the caller, so `workspace` below is never null here.
export function WorkspaceMembersPage() {
  const workspace = useCurrentWorkspace(null);
  if (!workspace) {
    return null;
  }

  return <MembersView workspaceId={workspace.id} workspaceName={workspace.name} isOwner={workspace.role === 'Owner'} />;
}

interface PendingRemoval {
  member: Member;
  isSelf: boolean;
}

function MembersView({
  workspaceId,
  workspaceName,
  isOwner,
}: {
  workspaceId: string;
  workspaceName: string;
  isOwner: boolean;
}) {
  const { user } = useAuth();
  const navigate = useNavigate();
  const members = useMembers(workspaceId);
  const changeRole = useChangeMemberRole(workspaceId);
  const removeMember = useRemoveMember(workspaceId);
  const [inviteOpen, setInviteOpen] = useState(false);
  const [pendingRemoval, setPendingRemoval] = useState<PendingRemoval | null>(null);

  async function confirmRemoval() {
    if (!pendingRemoval) {
      return;
    }

    try {
      await removeMember.mutateAsync(pendingRemoval.member.id);
    } catch {
      // useRemoveMember's onError already toasted the server's explanation; leave the dialog
      // open so the owner can see it land, instead of closing on a failed attempt.
      return;
    }

    const wasSelf = pendingRemoval.isSelf;
    setPendingRemoval(null);

    // The caller no longer belongs here — the workspace route would otherwise immediately show
    // "not found" underneath this very page.
    if (wasSelf) {
      navigate('/', { replace: true });
    }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-lg font-semibold text-slate-900">Members</h1>
        {isOwner && <Button onClick={() => setInviteOpen(true)}>Invite member</Button>}
      </div>

      {members.isPending && (
        <div className="space-y-2" aria-label="Loading members">
          <Skeleton className="h-12" />
          <Skeleton className="h-12" />
          <Skeleton className="h-12" />
        </div>
      )}

      {members.isError && (
        <ErrorState message="Could not load the members list." onRetry={() => void members.refetch()} />
      )}

      {members.isSuccess && members.data.length === 0 && (
        <EmptyState title="No members yet" description="Invite someone to start collaborating." />
      )}

      {members.isSuccess && members.data.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-slate-200">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-slate-200 bg-slate-50 text-xs uppercase text-slate-500">
              <tr>
                <th className="px-4 py-2" colSpan={2}>
                  Name
                </th>
                <th className="px-4 py-2">Email</th>
                <th className="px-4 py-2">Role</th>
                <th className="px-4 py-2">Joined</th>
                <th className="px-4 py-2">
                  <span className="sr-only">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {members.data.map((member) => {
                const isSelf = member.user.id === user?.id;
                const rolePending = changeRole.isPending && changeRole.variables?.memberId === member.id;
                const removePending = removeMember.isPending && removeMember.variables === member.id;

                return (
                  <tr key={member.id}>
                    <td className="px-4 py-2">
                      <Avatar name={member.user.displayName} imageUrl={member.user.avatarUrl} />
                    </td>
                    <td className="px-4 py-2 font-medium text-slate-900">{member.user.displayName}</td>
                    <td className="px-4 py-2 text-slate-600">{member.user.email}</td>
                    <td className="px-4 py-2">
                      {isOwner ? (
                        <select
                          aria-label={`Role for ${member.user.displayName}`}
                          value={member.role}
                          disabled={rolePending}
                          onChange={(event) =>
                            changeRole.mutate({ memberId: member.id, role: event.target.value as WorkspaceRole })
                          }
                          className="rounded-md border border-slate-300 px-2 py-1 text-sm disabled:bg-slate-50 focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-600"
                        >
                          <option value="Member">Member</option>
                          <option value="Owner">Owner</option>
                        </select>
                      ) : (
                        <span className="text-slate-700">{member.role}</span>
                      )}
                    </td>
                    <td className="px-4 py-2 text-slate-600">
                      <time dateTime={member.joinedAt} title={member.joinedAt}>
                        {formatJoinedDate(member.joinedAt)}
                      </time>
                    </td>
                    <td className="px-4 py-2 text-right">
                      {/* Hidden entirely, not disabled: a member has no action on anyone else's
                          row, and an owner acting on their own row leaves, same as anyone else. */}
                      {(isOwner || isSelf) && (
                        <Button
                          variant="ghost"
                          size="sm"
                          disabled={removePending}
                          onClick={() => setPendingRemoval({ member, isSelf })}
                        >
                          {isSelf ? 'Leave' : 'Remove'}
                        </Button>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {isOwner && <InviteMemberModal workspaceId={workspaceId} open={inviteOpen} onClose={() => setInviteOpen(false)} />}

      <ConfirmDialog
        open={pendingRemoval !== null}
        title={pendingRemoval?.isSelf ? 'Leave workspace' : 'Remove member'}
        description={
          pendingRemoval?.isSelf ? (
            <>
              Leave <strong>{workspaceName}</strong>? You will lose access to its projects and
              issues.
            </>
          ) : (
            <>
              Remove <strong>{pendingRemoval?.member.user.displayName}</strong> from{' '}
              <strong>{workspaceName}</strong>? They will immediately lose access to its projects
              and issues.
            </>
          )
        }
        confirmLabel={pendingRemoval?.isSelf ? 'Leave' : 'Remove'}
        pending={removeMember.isPending}
        onConfirm={() => void confirmRemoval()}
        onClose={() => setPendingRemoval(null)}
      />
    </div>
  );
}
