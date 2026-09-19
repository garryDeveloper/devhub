import { StyleSheet, Text, View } from 'react-native';
import { Screen } from '../../shared/components/Screen';
import { colors } from '../../shared/theme/colors';
import { spacing } from '../../shared/theme/spacing';
import { typography } from '../../shared/theme/typography';

export interface PlaceholderScreenProps {
  title: string;
  params?: Record<string, string>;
}

// Stands in for every screen that has its own ticket. This shell owns
// navigation and layout only — not the data on these screens.
export function PlaceholderScreen({ title, params }: PlaceholderScreenProps) {
  return (
    <Screen>
      <Text style={styles.title}>{title}</Text>
      <Text style={styles.subtitle}>This screen is built in its own ticket.</Text>
      {params && (
        <View style={styles.paramsBox}>
          {Object.entries(params).map(([key, value]) => (
            <Text key={key} style={styles.param}>
              {key}: {value}
            </Text>
          ))}
        </View>
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
    ...typography.body,
    color: colors.textMuted,
  },
  paramsBox: {
    marginTop: spacing.lg,
    padding: spacing.md,
    borderRadius: 8,
    backgroundColor: colors.surface,
    gap: spacing.xs,
  },
  param: {
    ...typography.caption,
    color: colors.textMuted,
  },
});
