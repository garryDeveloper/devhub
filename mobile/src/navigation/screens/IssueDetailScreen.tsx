import type { RouteProp } from '@react-navigation/native';
import { useRoute } from '@react-navigation/native';

import type { IssuesStackParamList } from '../types';
import { PlaceholderScreen } from './PlaceholderScreen';

export function IssueDetailScreen() {
  const route = useRoute<RouteProp<IssuesStackParamList, 'IssueDetail'>>();
  return (
    <PlaceholderScreen
      title="Issue"
      params={{ issueKey: route.params.issueKey }}
    />
  );
}
