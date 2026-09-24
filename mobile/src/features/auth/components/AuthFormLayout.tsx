import type { ReactNode } from 'react';
import {
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  StyleSheet,
  Text,
} from 'react-native';

import { colors } from '../../../shared/theme/colors';
import { spacing } from '../../../shared/theme/spacing';
import { typography } from '../../../shared/theme/typography';

// Keyboard-aware layout for the auth forms (DEVHUB-022), built from React Native primitives
// only — no keyboard library:
// - KeyboardAvoidingView pads the bottom on iOS so the keyboard does not cover the submit
//   button. Android resizes the window itself (adjustResize), so `behavior` is left unset there;
//   adding 'height' on top of that double-shrinks the view.
// - ScrollView lets a small phone reach every field while the keyboard is up, and
//   keyboardShouldPersistTaps="handled" makes the first tap on "Log in" submit instead of just
//   dismissing the keyboard.
// These screens sit under the AuthStack header, which already handles the top safe-area inset,
// so this deliberately does not use <Screen>.
export function AuthFormLayout({
  title,
  banner,
  children,
}: {
  title: string;
  banner?: string | null;
  children: ReactNode;
}) {
  return (
    <KeyboardAvoidingView
      style={styles.flex}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
    >
      <ScrollView
        style={styles.flex}
        contentContainerStyle={styles.content}
        keyboardShouldPersistTaps="handled"
      >
        <Text role="heading" style={styles.title}>
          {title}
        </Text>
        {banner ? (
          <Text role="alert" style={styles.banner}>
            {banner}
          </Text>
        ) : null}
        {children}
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  flex: {
    flex: 1,
    backgroundColor: colors.background,
  },
  content: {
    padding: spacing.lg,
    gap: spacing.lg,
  },
  title: {
    ...typography.title,
    color: colors.text,
  },
  banner: {
    ...typography.body,
    color: colors.danger,
    backgroundColor: colors.dangerSurface,
    borderRadius: 8,
    padding: spacing.md,
  },
});
