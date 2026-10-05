import { Image, StyleSheet, Text, View } from 'react-native';

import { colors } from '../theme/colors';
import { typography } from '../theme/typography';

export interface AvatarProps {
  name: string;
  imageUrl?: string | null;
}

function initials(name: string): string {
  const parts = name.trim().split(/\s+/);
  const first = parts[0]?.[0] ?? '';
  const last = parts.length > 1 ? parts[parts.length - 1]?.[0] ?? '' : '';
  return (first + last).toUpperCase();
}

// Port of web's Avatar (shared/components/Avatar.tsx). No user has a real avatarUrl until
// EPIC 15 — initials are the only case this renders today, but imageUrl is accepted now so
// nothing here changes later.
export function Avatar({ name, imageUrl }: AvatarProps) {
  if (imageUrl) {
    return (
      <Image
        source={{ uri: imageUrl }}
        accessible={false}
        style={styles.badge}
      />
    );
  }

  return (
    <View style={[styles.badge, styles.fallback]}>
      <Text style={styles.initials}>{initials(name)}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  badge: {
    width: 32,
    height: 32,
    borderRadius: 16,
  },
  fallback: {
    backgroundColor: colors.surface,
    alignItems: 'center',
    justifyContent: 'center',
  },
  initials: {
    ...typography.caption,
    fontWeight: '600',
    color: colors.textMuted,
  },
});
