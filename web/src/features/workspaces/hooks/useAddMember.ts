import { useMutation, useQueryClient } from '@tanstack/react-query';
import { qk } from '../../../shared/api/queryKeys';
import { addMember } from '../api/workspacesApi';
import type { Member, WorkspaceRole } from '../types';

export function useAddMember(workspaceId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ email, role }: { email: string; role: WorkspaceRole }) =>
      addMember(workspaceId, email, role),
    onSuccess: (created) => {
      queryClient.setQueryData<Member[]>(qk.members(workspaceId), (old) => [
        ...(old ?? []),
        created,
      ]);
      // memberCount on WorkspaceDto just changed too.
      void queryClient.invalidateQueries({ queryKey: qk.workspaces });
    },
  });
}
