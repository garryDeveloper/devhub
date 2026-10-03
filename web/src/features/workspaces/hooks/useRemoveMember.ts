import { useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiError } from '../../../shared/api/client';
import { qk } from '../../../shared/api/queryKeys';
import { useToast } from '../../../shared/hooks/useToast';
import { removeMember } from '../api/workspacesApi';
import type { Member } from '../types';

// Not optimistic: removal is confirmed before this ever fires (DEVHUB-028), so the extra wait
// for the server is the point, not a cost — there is no snappy-dropdown case to protect here.
// The one expected failure (removing the last owner, 422) is surfaced with the server's own
// sentence, same as useChangeMemberRole; the confirm dialog stays open so the owner can see why.
export function useRemoveMember(workspaceId: string) {
  const queryClient = useQueryClient();
  const toast = useToast();

  return useMutation({
    mutationFn: (memberId: string) => removeMember(workspaceId, memberId),
    onSuccess: (_void, memberId) => {
      queryClient.setQueryData<Member[]>(qk.members(workspaceId), (old) =>
        old?.filter((member) => member.id !== memberId),
      );
      void queryClient.invalidateQueries({ queryKey: qk.workspaces });
    },
    onError: (error) => {
      const message =
        error instanceof ApiError
          ? error.detail ?? error.title
          : 'Could not remove the member. Please try again.';
      toast.show(message, 'error');
    },
  });
}
