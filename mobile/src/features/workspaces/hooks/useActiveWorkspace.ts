import { useContext } from 'react';

import { ActiveWorkspaceContext } from '../context/activeWorkspaceContext';
import type { ActiveWorkspaceContextValue } from '../context/activeWorkspaceContext';

export function useActiveWorkspace(): ActiveWorkspaceContextValue {
  const ctx = useContext(ActiveWorkspaceContext);
  if (!ctx) {
    throw new Error(
      'useActiveWorkspace must be used within an ActiveWorkspaceProvider',
    );
  }
  return ctx;
}
