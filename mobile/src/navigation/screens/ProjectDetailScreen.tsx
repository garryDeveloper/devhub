import type { RouteProp } from '@react-navigation/native';
import { useRoute } from '@react-navigation/native';

import type { ProjectsStackParamList } from '../types';
import { PlaceholderScreen } from './PlaceholderScreen';

export function ProjectDetailScreen() {
  const route = useRoute<RouteProp<ProjectsStackParamList, 'ProjectDetail'>>();
  return (
    <PlaceholderScreen
      title="Project"
      params={{ projectId: route.params.projectId }}
    />
  );
}
