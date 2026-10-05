import { useQuery } from '@tanstack/react-query';

import { qk } from '../../../shared/api/queryKeys';
import { fetchWorkspaces } from '../api/workspacesApi';

// The one source of "which workspaces can I see". Server data stays in the query cache, never in
// a global store (CLAUDE.md §4).
export function useWorkspaces() {
  return useQuery({ queryKey: qk.workspaces, queryFn: fetchWorkspaces });
}
