import { FlatList, Pressable, StyleSheet, Text, View } from 'react-native';

import { ErrorView } from '../../../shared/components/ErrorView';
import { ListEmpty } from '../../../shared/components/ListEmpty';
import { Loading } from '../../../shared/components/Loading';
import { colors } from '../../../shared/theme/colors';
import { spacing } from '../../../shared/theme/spacing';
import { typography } from '../../../shared/theme/typography';
import { useActiveWorkspace } from '../hooks/useActiveWorkspace';
import { useWorkspaces } from '../hooks/useWorkspaces';
import type { Workspace } from '../types';

export interface WorkspaceSelectorProps {
  onClose: () => void;
}

// Content of the BottomSheet opened from the Projects tab header (DEVHUB-029). Creating,
// inviting and renaming all stay on web by design — this is a picker, nothing else.
export function WorkspaceSelector({ onClose }: WorkspaceSelectorProps) {
  const workspaces = useWorkspaces();
  const { activeWorkspace, setActiveWorkspace } = useActiveWorkspace();

  function choose(workspace: Workspace) {
    setActiveWorkspace(workspace);
    onClose();
  }

  return (
    <View>
      <Text style={styles.title}>Switch workspace</Text>

      {workspaces.isPending && <Loading />}

      {workspaces.isError && (
        <ErrorView
          message="Could not load your workspaces."
          onRetry={() => void workspaces.refetch()}
        />
      )}

      {workspaces.isSuccess && workspaces.data.length === 0 && (
        <ListEmpty
          title="No workspace yet"
          description="Create one on the DevHub web app to get started."
        />
      )}

      {workspaces.isSuccess && workspaces.data.length > 0 && (
        <FlatList
          data={workspaces.data}
          keyExtractor={(workspace) => workspace.id}
          style={styles.list}
          renderItem={({ item }) => {
            const isCurrent = item.id === activeWorkspace?.id;
            return (
              <Pressable
                onPress={() => choose(item)}
                accessibilityRole="button"
                accessibilityLabel={`${item.name}, ${item.role}`}
                accessibilityState={{ selected: isCurrent }}
                style={styles.row}
              >
                <View style={styles.rowText}>
                  <Text style={styles.rowName}>
                    {isCurrent ? '✓ ' : ''}
                    {item.name}
                  </Text>
                  <Text style={styles.rowRole}>{item.role}</Text>
                </View>
              </Pressable>
            );
          }}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  title: {
    ...typography.title,
    color: colors.text,
    marginBottom: spacing.md,
  },
  list: {
    maxHeight: 320,
  },
  row: {
    minHeight: 48,
    justifyContent: 'center',
    paddingVertical: spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  rowText: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  rowName: {
    ...typography.bodyStrong,
    color: colors.text,
  },
  rowRole: {
    ...typography.caption,
    color: colors.textMuted,
  },
});
