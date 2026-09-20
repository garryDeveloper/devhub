import type { RouteProp } from '@react-navigation/native';
import { useRoute } from '@react-navigation/native';

import type { ProjectsStackParamList } from '../types';
import { PlaceholderScreen } from './PlaceholderScreen';

export function DeploymentDetailScreen() {
  const route =
    useRoute<RouteProp<ProjectsStackParamList, 'DeploymentDetail'>>();
  return (
    <PlaceholderScreen
      title="Deployment"
      params={{ deploymentId: route.params.deploymentId }}
    />
  );
}
