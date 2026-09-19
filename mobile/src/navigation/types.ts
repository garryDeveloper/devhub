import type { NavigatorScreenParams } from '@react-navigation/native';

export type AuthStackParamList = {
  Welcome: undefined;
  Login: undefined;
  Register: undefined;
};

export type HomeStackParamList = {
  Home: undefined;
};

export type IssuesStackParamList = {
  MyIssues: undefined;
  IssueDetail: { issueKey: string };
};

export type ProjectsStackParamList = {
  ProjectList: undefined;
  ProjectDetail: { projectId: string };
  DeploymentDetail: { deploymentId: string };
};

export type MeStackParamList = {
  Profile: undefined;
  ApiHealth: undefined;
};

export type AppTabParamList = {
  HomeTab: NavigatorScreenParams<HomeStackParamList>;
  IssuesTab: NavigatorScreenParams<IssuesStackParamList>;
  ProjectsTab: NavigatorScreenParams<ProjectsStackParamList>;
  MeTab: NavigatorScreenParams<MeStackParamList>;
};

export type RootStackParamList = {
  Auth: NavigatorScreenParams<AuthStackParamList>;
  App: NavigatorScreenParams<AppTabParamList>;
};

// Lets useNavigation()/useRoute() infer types everywhere without repeating
// generics at every call site (React Navigation's recommended pattern).
declare global {
  namespace ReactNavigation {
    // eslint-disable-next-line @typescript-eslint/no-empty-object-type -- namespace merging requires `interface`, not a type alias
    interface RootParamList extends RootStackParamList {}
  }
}
