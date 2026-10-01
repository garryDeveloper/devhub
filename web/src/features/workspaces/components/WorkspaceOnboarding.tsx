import { useState } from 'react';
import { Button } from '../../../shared/components/Button';
import { EmptyState } from '../../../shared/components/EmptyState';
import { CreateWorkspaceModal } from './CreateWorkspaceModal';

// What a brand-new account sees at `/`: one explanation and one action (user-flows.md Flow 1,
// "Register → Create workspace"). Rendered inside the shell, so the app never looks broken.
export function WorkspaceOnboarding() {
  const [createOpen, setCreateOpen] = useState(false);

  return (
    <div className="mx-auto max-w-lg pt-12">
      <EmptyState
        title="Create your first workspace"
        description="A workspace holds your team's projects, issues and deployments. You can invite people once it exists."
        action={
          <Button onClick={() => setCreateOpen(true)}>Create workspace</Button>
        }
      />
      <CreateWorkspaceModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
      />
    </div>
  );
}
