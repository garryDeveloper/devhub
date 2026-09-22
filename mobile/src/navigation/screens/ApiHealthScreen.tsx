import { useQuery } from '@tanstack/react-query';
import { StyleSheet, Text } from 'react-native';

import { apiFetch } from '../../shared/api/client';
import { qk } from '../../shared/api/queryKeys';
import { ErrorView } from '../../shared/components/ErrorView';
import { Loading } from '../../shared/components/Loading';
import { Screen } from '../../shared/components/Screen';
import { colors } from '../../shared/theme/colors';
import { spacing } from '../../shared/theme/spacing';
import { typography } from '../../shared/theme/typography';

interface HealthResponse {
  status: string;
}

// The one data fetch this shell owns — verifies EXPO_PUBLIC_API_URL and the
// API client are wired up (task: "verify a /health call succeeds ... over the LAN").
export function ApiHealthScreen() {
  const { data, error, isPending, refetch } = useQuery({
    queryKey: qk.health,
    queryFn: () => apiFetch<HealthResponse>('/health'),
  });

  return (
    <Screen>
      <Text style={styles.title}>API health</Text>
      <Text style={styles.subtitle}>
        Checks{' '}
        {process.env.EXPO_PUBLIC_API_URL ?? '(EXPO_PUBLIC_API_URL not set)'}
      </Text>

      {isPending && <Loading />}
      {error && (
        <ErrorView
          message={error instanceof Error ? error.message : 'Request failed'}
          onRetry={() => refetch()}
        />
      )}
      {data && (
        <Text style={styles.result}>{JSON.stringify(data, null, 2)}</Text>
      )}
    </Screen>
  );
}

const styles = StyleSheet.create({
  title: {
    ...typography.title,
    color: colors.text,
    marginBottom: spacing.xs,
  },
  subtitle: {
    ...typography.caption,
    color: colors.textMuted,
    marginBottom: spacing.lg,
  },
  result: {
    ...typography.caption,
    color: colors.text,
    backgroundColor: colors.surface,
    padding: spacing.md,
    borderRadius: 8,
  },
});
