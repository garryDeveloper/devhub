import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useNavigation } from '@react-navigation/native';
import { Pressable, StyleSheet, Text } from 'react-native';
import { Screen } from '../../shared/components/Screen';
import { colors } from '../../shared/theme/colors';
import { spacing } from '../../shared/theme/spacing';
import { typography } from '../../shared/theme/typography';
import type { MeStackParamList } from '../types';

export function ProfileScreen() {
  const navigation = useNavigation<NativeStackNavigationProp<MeStackParamList, 'Profile'>>();

  return (
    <Screen>
      <Text style={styles.title}>Me</Text>
      <Text style={styles.subtitle}>Profile and account settings are built in their own ticket.</Text>
      <Pressable
        onPress={() => navigation.navigate('ApiHealth')}
        style={styles.link}
        accessibilityRole="button"
      >
        <Text style={styles.linkText}>Check API health</Text>
      </Pressable>
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
  link: {
    marginTop: spacing.lg,
    minHeight: 44,
    justifyContent: 'center',
  },
  linkText: {
    ...typography.bodyStrong,
    color: colors.primary,
  },
});
