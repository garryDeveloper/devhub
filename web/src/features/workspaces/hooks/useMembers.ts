import { useQuery } from '@tanstack/react-query';
import { qk } from '../../../shared/api/queryKeys';
import { fetchMembers } from '../api/workspacesApi';

export function useMembers(workspaceId: string) {
  return useQuery({
    queryKey: qk.members(workspaceId),
    queryFn: () => fetchMembers(workspaceId),
  });
}
