import { useState } from 'react';
import { Pressable, StyleSheet, Text } from 'react-native';

import { WorkspaceSelector } from './WorkspaceSelector';
import { BottomSheet } from '../../../shared/components/BottomSheet';
import { colors } from '../../../shared/theme/colors';
import { spacing } from '../../../shared/theme/spacing';
import { typography } from '../../../shared/theme/typography';
import { useActiveWorkspace } from '../hooks/useActiveWorkspace';

// Projects tab header button (DEVHUB-029 task 1). Reuses the shared BottomSheet used elsewhere
// for pickers, instead of a modal nav screen with no back/forward state of its own.
export function WorkspaceSelectorButton() {
  const [open, setOpen] = useState(false);
  const { activeWorkspace } = useActiveWorkspace();

  return (
    <>
      <Pressable
        onPress={() => setOpen(true)}
        accessibilityRole="button"
        accessibilityLabel={`Workspace: ${activeWorkspace?.name ?? 'None'}`}
        style={styles.button}
      >
        <Text style={styles.label} numberOfLines={1}>
          {activeWorkspace?.name ?? 'Workspace'}
        </Text>
        <Text style={styles.chevron}>▾</Text>
      </Pressable>

      <BottomSheet visible={open} onClose={() => setOpen(false)}>
        <WorkspaceSelector onClose={() => setOpen(false)} />
      </BottomSheet>
    </>
  );
}

const styles = StyleSheet.create({
  button: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.xs,
    minHeight: 44,
    maxWidth: 160,
    paddingHorizontal: spacing.sm,
  },
  label: {
    ...typography.bodyStrong,
    color: colors.primary,
  },
  chevron: {
    color: colors.textMuted,
  },
});
