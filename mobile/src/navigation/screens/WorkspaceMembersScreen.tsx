import { FlatList, StyleSheet, Text, View } from 'react-native';

import { useActiveWorkspace } from '../../features/workspaces/hooks/useActiveWorkspace';
import { useMembers } from '../../features/workspaces/hooks/useMembers';
import type { Member } from '../../features/workspaces/types';
import { Avatar } from '../../shared/components/Avatar';
import { ErrorView } from '../../shared/components/ErrorView';
import { ListEmpty } from '../../shared/components/ListEmpty';
import { Loading } from '../../shared/components/Loading';
import { Screen } from '../../shared/components/Screen';
import { colors } from '../../shared/theme/colors';
import { spacing } from '../../shared/theme/spacing';
import { typography } from '../../shared/theme/typography';

// Me tab → Workspace members (DEVHUB-029). Read-only: no invite, role-change or remove control
// appears anywhere here — that administration stays on web by design.
export function WorkspaceMembersScreen() {
  const { activeWorkspace } = useActiveWorkspace();

  if (!activeWorkspace) {
    return (
      <Screen>
        <ListEmpty
          title="No workspace yet"
          description="Create one on the DevHub web app to get started."
        />
      </Screen>
    );
  }

  return <MembersList workspaceId={activeWorkspace.id} />;
}

function MembersList({ workspaceId }: { workspaceId: string }) {
  const members = useMembers(workspaceId);

  return (
    <Screen>
      {members.isPending && <Loading />}

      {members.isError && (
        <ErrorView
          message="Could not load the members list."
          onRetry={() => void members.refetch()}
        />
      )}

      {members.isSuccess && (
        <FlatList
          data={members.data}
          keyExtractor={(member) => member.id}
          refreshing={members.isRefetching}
          onRefresh={() => void members.refetch()}
          ListEmptyComponent={
            <ListEmpty
              title="No members yet"
              description="Invite someone on the DevHub web app to start collaborating."
            />
          }
          renderItem={({ item }) => <MemberRow member={item} />}
        />
      )}
    </Screen>
  );
}

function MemberRow({ member }: { member: Member }) {
  return (
    <View style={styles.row}>
      <Avatar name={member.user.displayName} imageUrl={member.user.avatarUrl} />
      <View style={styles.rowText}>
        <Text style={styles.name}>{member.user.displayName}</Text>
        <Text style={styles.email}>{member.user.email}</Text>
      </View>
      <Text style={styles.role}>{member.role}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    minHeight: 56,
    paddingVertical: spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  rowText: {
    flex: 1,
  },
  name: {
    ...typography.bodyStrong,
    color: colors.text,
  },
  email: {
    ...typography.caption,
    color: colors.textMuted,
  },
  role: {
    ...typography.caption,
    color: colors.textMuted,
  },
});
