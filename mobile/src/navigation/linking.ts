import type { LinkingOptions } from '@react-navigation/native';
import * as Linking from 'expo-linking';
import type { RootStackParamList } from './types';

// devhub://projects/{id}, devhub://issues/{key}, devhub://deployments/{id} —
// used by notifications (mobile-architecture.md §3).
//
// Note: the custom `devhub://` scheme only opens the app in a standalone or
// dev-client build. In Expo Go, use the generated `exp://…` prefix below
// (Linking.createURL) to test deep links during development.
export const linking: LinkingOptions<RootStackParamList> = {
  prefixes: [Linking.createURL('/'), 'devhub://'],
  config: {
    screens: {
      App: {
        screens: {
          ProjectsTab: {
            screens: {
              ProjectList: 'projects',
              ProjectDetail: 'projects/:projectId',
              DeploymentDetail: 'deployments/:deploymentId',
            },
          },
          IssuesTab: {
            screens: {
              MyIssues: 'issues',
              IssueDetail: 'issues/:issueKey',
            },
          },
          HomeTab: {
            screens: {
              Home: 'home',
            },
          },
          MeTab: {
            screens: {
              Profile: 'me',
              ApiHealth: 'me/health',
            },
          },
        },
      },
      Auth: {
        screens: {
          Welcome: 'welcome',
          Login: 'login',
          Register: 'register',
        },
      },
    },
  },
};
