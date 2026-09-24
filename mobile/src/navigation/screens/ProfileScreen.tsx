import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import { useAuth } from '../../features/auth/hooks/useAuth';
import { Button } from '../../shared/components/Button';
import { Screen } from '../../shared/components/Screen';
import { colors } from '../../shared/theme/colors';
import { spacing } from '../../shared/theme/spacing';
import { typography } from '../../shared/theme/typography';
import type { MeStackParamList } from '../types';

export function ProfileScreen() {
  const navigation =
    useNavigation<NativeStackNavigationProp<MeStackParamList, 'Profile'>>();
  const { user, logout } = useAuth();

  return (
    <Screen>
      <Text style={styles.title}>{user?.displayName ?? 'Me'}</Text>
      <Text style={styles.subtitle}>{user?.email}</Text>
      <Pressable
        onPress={() => navigation.navigate('ApiHealth')}
        style={styles.link}
        accessibilityRole="button"
      >
        <Text style={styles.linkText}>Check API health</Text>
      </Pressable>
      {/* No navigation call: logout() clears `user`, and RootNavigator swaps AppTabs for
          AuthStack (Welcome), with nothing left in history to go back to. */}
      <View style={styles.logout}>
        <Button
          label="Log out"
          variant="secondary"
          onPress={() => void logout()}
        />
      </View>
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
  logout: {
    marginTop: 'auto',
    paddingBottom: spacing.lg,
  },
});
