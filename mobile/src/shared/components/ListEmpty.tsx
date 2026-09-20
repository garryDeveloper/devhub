import { StyleSheet, Text, View } from 'react-native';

import { colors } from '../theme/colors';
import { spacing } from '../theme/spacing';
import { typography } from '../theme/typography';

export interface ListEmptyProps {
  title: string;
  description?: string;
}

// Explanatory copy, not a blank screen (mobile-architecture.md §6).
export function ListEmpty({ title, description }: ListEmptyProps) {
  return (
    <View style={styles.container}>
      <Text style={styles.title}>{title}</Text>
      {description && <Text style={styles.description}>{description}</Text>}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    alignItems: 'center',
    gap: spacing.xs,
    paddingVertical: spacing.xxl,
  },
  title: {
    ...typography.bodyStrong,
    color: colors.text,
  },
  description: {
    ...typography.caption,
    color: colors.textMuted,
    textAlign: 'center',
  },
});
