import { useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiError } from '../../../shared/api/client';
import { qk } from '../../../shared/api/queryKeys';
import { useToast } from '../../../shared/hooks/useToast';
import { changeMemberRole } from '../api/workspacesApi';
import type { Member, WorkspaceRole } from '../types';

export interface ChangeMemberRoleVariables {
  memberId: string;
  role: WorkspaceRole;
}

// Optimistic so the dropdown reads back the chosen role instantly; rolled back on failure
// (DEVHUB-028). The one expected failure — demoting the last owner — is a 422 whose `detail` is
// already the humane sentence the ticket asks for, so it is shown as-is rather than papered over
// with a generic message.
export function useChangeMemberRole(workspaceId: string) {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: ({ memberId, role }: ChangeMemberRoleVariables) =>
      changeMemberRole(workspaceId, memberId, role),
    onMutate: async ({ memberId, role }) => {
      await queryClient.cancelQueries({ queryKey: qk.members(workspaceId) });
      const previous = queryClient.getQueryData<Member[]>(qk.members(workspaceId));

      queryClient.setQueryData<Member[]>(qk.members(workspaceId), (old) =>
        old?.map((member) => (member.id === memberId ? { ...member, role } : member)),
      );

      return { previous };
    },
    onError: (error, _variables, context) => {
      if (context?.previous) {
        queryClient.setQueryData(qk.members(workspaceId), context.previous);
      }

      const message =
        error instanceof ApiError
          ? error.detail ?? error.title
          : 'Could not change the role. Please try again.';
      toast.show(message, 'error');
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: qk.members(workspaceId) });
      // Covers the one case where this changes the caller's own role too (another owner
      // promotes/demotes them): WorkspaceDto.role is the caller's role, and the switcher reads it.
      void queryClient.invalidateQueries({ queryKey: qk.workspaces });
    },
  });
}
