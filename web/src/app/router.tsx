import { createBrowserRouter } from 'react-router-dom';
import { AppShell } from './layouts/AppShell';
import { AuthLayout } from './layouts/AuthLayout';
import { HealthCheckPage } from './routes/HealthCheckPage';
import { NotFoundPage } from './routes/NotFoundPage';
import { PlaceholderPage } from './routes/PlaceholderPage';
import { RouteErrorBoundary } from './routes/RouteErrorBoundary';

// Route tree mirrors docs/screens-and-navigation.md §2. Every screen is a
// placeholder here — real screens land in their own tickets.
export const router = createBrowserRouter([
  {
    element: <AppShell />,
    errorElement: <RouteErrorBoundary />,
    children: [
      { index: true, element: <PlaceholderPage title="Overview" /> },
      { path: 'w/:workspaceSlug', element: <PlaceholderPage title="Workspace dashboard" /> },
      { path: 'w/:workspaceSlug/settings', element: <PlaceholderPage title="Workspace settings" /> },
      { path: 'w/:workspaceSlug/members', element: <PlaceholderPage title="Workspace members" /> },
      { path: 'w/:workspaceSlug/projects', element: <PlaceholderPage title="Project list" /> },
      { path: 'p/:projectKey', element: <PlaceholderPage title="Project overview" /> },
      { path: 'p/:projectKey/issues', element: <PlaceholderPage title="Issues" /> },
      { path: 'p/:projectKey/issues/:issueKey', element: <PlaceholderPage title="Issue detail" /> },
      { path: 'p/:projectKey/board', element: <PlaceholderPage title="Board" /> },
      { path: 'p/:projectKey/releases', element: <PlaceholderPage title="Releases" /> },
      { path: 'p/:projectKey/releases/:version', element: <PlaceholderPage title="Release detail" /> },
      { path: 'p/:projectKey/environments', element: <PlaceholderPage title="Environments" /> },
      { path: 'p/:projectKey/environments/:envId', element: <PlaceholderPage title="Environment detail" /> },
      { path: 'p/:projectKey/deployments/:number', element: <PlaceholderPage title="Deployment detail" /> },
      { path: 'p/:projectKey/cicd', element: <PlaceholderPage title="CI/CD runs" /> },
      { path: 'p/:projectKey/activity', element: <PlaceholderPage title="Project activity" /> },
      { path: 'p/:projectKey/settings', element: <PlaceholderPage title="Project settings" /> },
      { path: 'notifications', element: <PlaceholderPage title="Notifications" /> },
      { path: 'account/profile', element: <PlaceholderPage title="Profile" /> },
      { path: 'account/settings', element: <PlaceholderPage title="Account settings" /> },
      { path: 'debug/health', element: <HealthCheckPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
  {
    element: <AuthLayout />,
    errorElement: <RouteErrorBoundary />,
    children: [
      { path: 'login', element: <PlaceholderPage title="Login" /> },
      { path: 'register', element: <PlaceholderPage title="Register" /> },
      { path: 'forgot-password', element: <PlaceholderPage title="Forgot password" /> },
    ],
  },
]);
