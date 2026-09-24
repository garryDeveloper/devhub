import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { StyleSheet, Text, View } from 'react-native';

import type { AuthStackParamList } from '../../../navigation/types';
import { Button } from '../../../shared/components/Button';
import { Screen } from '../../../shared/components/Screen';
import { colors } from '../../../shared/theme/colors';
import { spacing } from '../../../shared/theme/spacing';
import { typography } from '../../../shared/theme/typography';
import { useAuth } from '../hooks/useAuth';

export function WelcomeScreen() {
  const navigation =
    useNavigation<NativeStackNavigationProp<AuthStackParamList, 'Welcome'>>();
  const { sessionExpired } = useAuth();

  return (
    <Screen>
      <View style={styles.hero}>
        <Text role="heading" style={styles.title}>
          DevHub
        </Text>
        <Text style={styles.subtitle}>
          What is being worked on, what was shipped, and is production healthy?
        </Text>
      </View>

      {/* Informational, not an error: the user did nothing wrong (DEVHUB-021 parity). */}
      {sessionExpired ? (
        <Text role="status" style={styles.notice}>
          Your session expired. Please log in again.
        </Text>
      ) : null}

      <View style={styles.actions}>
        <Button label="Log in" onPress={() => navigation.navigate('Login')} />
        <Button
          label="Create an account"
          variant="secondary"
          onPress={() => navigation.navigate('Register')}
        />
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  hero: {
    flex: 1,
    justifyContent: 'center',
    gap: spacing.sm,
  },
  title: {
    ...typography.title,
    fontSize: 28,
    color: colors.text,
  },
  subtitle: {
    ...typography.body,
    color: colors.textMuted,
  },
  notice: {
    ...typography.body,
    color: colors.textMuted,
    backgroundColor: colors.surface,
    borderRadius: 8,
    padding: spacing.md,
    marginBottom: spacing.lg,
  },
  actions: {
    gap: spacing.md,
    paddingBottom: spacing.xl,
  },
});
