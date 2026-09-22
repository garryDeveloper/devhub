import type { ReactNode } from 'react';
import { Modal, Pressable, StyleSheet, View } from 'react-native';

import { colors } from '../theme/colors';
import { spacing } from '../theme/spacing';

export interface BottomSheetProps {
  visible: boolean;
  onClose: () => void;
  children: ReactNode;
}

// Status/priority changes open a bottom sheet, not a dropdown (mobile-architecture.md
// §6). Plain RN Modal, no gesture library — a heavier drag-to-dismiss sheet can
// replace this later if a screen actually needs one (no heavyweight kit up front).
export function BottomSheet({ visible, onClose, children }: BottomSheetProps) {
  return (
    <Modal
      visible={visible}
      transparent
      animationType="slide"
      onRequestClose={onClose}
    >
      <View style={styles.overlay}>
        <Pressable
          style={StyleSheet.absoluteFill}
          onPress={onClose}
          accessibilityRole="button"
          accessibilityLabel="Close"
        />
        <View style={styles.sheet}>{children}</View>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  overlay: {
    flex: 1,
    justifyContent: 'flex-end',
    backgroundColor: 'rgba(15, 23, 42, 0.4)',
  },
  sheet: {
    backgroundColor: colors.background,
    borderTopLeftRadius: 16,
    borderTopRightRadius: 16,
    padding: spacing.lg,
    paddingBottom: spacing.xxl,
  },
});
