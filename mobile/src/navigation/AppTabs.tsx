import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { Text } from 'react-native';

import { ApiHealthScreen } from './screens/ApiHealthScreen';
import { DeploymentDetailScreen } from './screens/DeploymentDetailScreen';
import { IssueDetailScreen } from './screens/IssueDetailScreen';
import { PlaceholderScreen } from './screens/PlaceholderScreen';
import { ProfileScreen } from './screens/ProfileScreen';
import { ProjectDetailScreen } from './screens/ProjectDetailScreen';
import type {
  AppTabParamList,
  HomeStackParamList,
  IssuesStackParamList,
  MeStackParamList,
  ProjectsStackParamList,
} from './types';
import { colors } from '../shared/theme/colors';

const Tab = createBottomTabNavigator<AppTabParamList>();
const HomeStack = createNativeStackNavigator<HomeStackParamList>();
const IssuesStack = createNativeStackNavigator<IssuesStackParamList>();
const ProjectsStack = createNativeStackNavigator<ProjectsStackParamList>();
const MeStack = createNativeStackNavigator<MeStackParamList>();

function HomeTabNavigator() {
  return (
    <HomeStack.Navigator>
      <HomeStack.Screen name="Home" options={{ title: 'Home' }}>
        {() => <PlaceholderScreen title="Home" />}
      </HomeStack.Screen>
    </HomeStack.Navigator>
  );
}

function IssuesTabNavigator() {
  return (
    <IssuesStack.Navigator>
      <IssuesStack.Screen name="MyIssues" options={{ title: 'Issues' }}>
        {() => <PlaceholderScreen title="My issues" />}
      </IssuesStack.Screen>
      <IssuesStack.Screen
        name="IssueDetail"
        component={IssueDetailScreen}
        options={{ title: 'Issue' }}
      />
    </IssuesStack.Navigator>
  );
}

function ProjectsTabNavigator() {
  return (
    <ProjectsStack.Navigator>
      <ProjectsStack.Screen name="ProjectList" options={{ title: 'Projects' }}>
        {() => <PlaceholderScreen title="Projects" />}
      </ProjectsStack.Screen>
      <ProjectsStack.Screen
        name="ProjectDetail"
        component={ProjectDetailScreen}
        options={{ title: 'Project' }}
      />
      <ProjectsStack.Screen
        name="DeploymentDetail"
        component={DeploymentDetailScreen}
        options={{ title: 'Deployment' }}
      />
    </ProjectsStack.Navigator>
  );
}

function MeTabNavigator() {
  return (
    <MeStack.Navigator>
      <MeStack.Screen
        name="Profile"
        component={ProfileScreen}
        options={{ title: 'Me' }}
      />
      <MeStack.Screen
        name="ApiHealth"
        component={ApiHealthScreen}
        options={{ title: 'API health' }}
      />
    </MeStack.Navigator>
  );
}

const tabIcons: Record<keyof AppTabParamList, string> = {
  HomeTab: '⌂',
  IssuesTab: '☰',
  ProjectsTab: '▣',
  MeTab: '👤',
};

// Bottom tab navigation per docs/screens-and-navigation.md §5 — mobile has its
// own IA, not a port of the desktop sidebar.
export function AppTabs() {
  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        headerShown: false,
        tabBarActiveTintColor: colors.primary,
        tabBarInactiveTintColor: colors.textMuted,
        tabBarIcon: () => <Text>{tabIcons[route.name]}</Text>,
      })}
    >
      <Tab.Screen
        name="HomeTab"
        component={HomeTabNavigator}
        options={{ title: 'Home' }}
      />
      <Tab.Screen
        name="IssuesTab"
        component={IssuesTabNavigator}
        options={{ title: 'Issues' }}
      />
      <Tab.Screen
        name="ProjectsTab"
        component={ProjectsTabNavigator}
        options={{ title: 'Projects' }}
      />
      <Tab.Screen
        name="MeTab"
        component={MeTabNavigator}
        options={{ title: 'Me' }}
      />
    </Tab.Navigator>
  );
}
