# Screens & navigation

Information architecture, routes and screen inventory for web and mobile.

---

## 1. Web information architecture

```text
App
├── Workspace switcher
├── Dashboard (workspace overview)
├── Projects
│   └── Project
│       ├── Overview (project dashboard)
│       ├── Issues (list)
│       ├── Board (kanban)
│       ├── Releases
│       ├── Environments
│       ├── Deployments
│       ├── CI/CD
│       ├── Activity
│       └── Settings (general, members, labels)
├── Search (⌘/Ctrl + K)
├── Notifications
└── Account
    ├── Profile
    └── Settings
```

## 2. Web routes

| Route | Screen | Auth |
|---|---|---|
| `/login` | Login | public |
| `/register` | Register | public |
| `/forgot-password` | Forgot password | public |
| `/` | Redirect to last workspace | private |
| `/w/:workspaceSlug` | Workspace dashboard | private |
| `/w/:workspaceSlug/settings` | Workspace settings | owner |
| `/w/:workspaceSlug/members` | Members + invite modal | owner |
| `/w/:workspaceSlug/projects` | Project list | private |
| `/p/:projectKey` | Project overview / dashboard | member |
| `/p/:projectKey/issues` | Issue list (filters in query string) | member |
| `/p/:projectKey/issues/:issueKey` | Issue detail (drawer over list) | member |
| `/p/:projectKey/board` | Kanban board | member |
| `/p/:projectKey/releases` | Releases list | member |
| `/p/:projectKey/releases/:version` | Release detail | member |
| `/p/:projectKey/environments` | Environments | member |
| `/p/:projectKey/environments/:envId` | Environment + deployment history | member |
| `/p/:projectKey/deployments/:number` | Deployment detail + timeline | member |
| `/p/:projectKey/cicd` | CI/CD runs | member |
| `/p/:projectKey/activity` | Project activity feed | member |
| `/p/:projectKey/settings` | Project settings, members, labels | owner/admin |
| `/notifications` | Notification center | private |
| `/account/profile` | Profile | private |
| `/account/settings` | Account settings | private |

Rules:

- Filters live in the URL query string so a filtered view is shareable and back/forward works.
- The issue detail is a **drawer** over the list on desktop and a full page on narrow viewports;
  the URL is the same either way.
- Unknown route → 404 screen inside the app shell, not a blank page.

## 3. Desktop sidebar

```text
DEVHUB                       [workspace switcher ▾]

⌂ Overview
▣ Projects
  └── DevHub
      ├── Issues
      ├── Board
      ├── Releases
      ├── Environments
      ├── Deployments
      └── CI/CD

⌕ Search                     ⌘K

──────────────────────────
🔔 Notifications  (badge)
⚙ Settings
👤 Profile
```

## 4. Web screen inventory

| Epic | Screen | Key elements |
|---|---|---|
| 2 | Login / Register / Forgot password | form, inline validation, error summary |
| 2 | Profile / Account settings | display name, avatar upload, password change |
| 3 | Workspace switcher | dropdown, create workspace, recent |
| 3 | Workspace settings / Members | role dropdown, invite modal, remove confirm |
| 4 | Project list | grid/list, key + color, archived filter |
| 4 | Project settings | name, key (read-only after create), color, icon, archive |
| 5 | Issue list | table, filter bar, sort, pagination, bulk-free MVP |
| 5 | Board | 6 columns, drag & drop, optimistic status update, WIP counts |
| 5 | Issue detail drawer | title, description (markdown), status/priority/assignee/labels/due, activity + comments |
| 5 | Create issue modal | title, description, priority, assignee, labels |
| 6 | Filter bar | status, priority, assignee, label, date, saved presets |
| 8 | Command palette (⌘K) | debounced search across issues/projects/releases, keyboard nav |
| 9 | Environments | one card per env: health dot, version, last deploy relative time |
| 10 | Deployment history | status glyph, version, short sha, relative time, duration |
| 10 | Deployment detail | metadata + event timeline, link to CI run and release |
| 11 | Releases list / detail | version, status, published date, linked issues, deployment |
| 12 | CI/CD runs | workflow, branch, sha, status, duration, external link |
| 13 | Project dashboard | env health banner, counters, recent deployments, recent CI runs, recent activity |
| 14 | Notification center | unread first, mark read / mark all read, deep links |
| 15 | Attachment uploader | drag & drop, progress, size/type validation |

Every data screen ships **loading / empty / error** states. Empty states explain the next action
("No issues yet — create the first one"), they are not blank boxes.

---

## 5. Mobile information architecture

Mobile is not a port of the desktop UI. It optimizes for: checking status, reading and updating
issues, commenting, and checking deployments. Complex configuration stays on web.

Bottom tab navigation:

```text
┌─────────────────────────────┐
│                             │
│        Screen content       │
│                             │
├─────────────────────────────┤
│ Home  Issues  Projects  Me  │
└─────────────────────────────┘
```

## 6. Mobile navigation tree

```text
RootNavigator
├── AuthStack (unauthenticated)
│   ├── Welcome
│   ├── Login
│   └── Register
└── AppTabs (authenticated)
    ├── HomeTab
    │   ├── Home (workspace + project health summary)
    │   └── NotificationsScreen
    ├── IssuesTab
    │   ├── MyIssues
    │   ├── IssueDetail
    │   ├── IssueEdit
    │   └── IssueFilters (modal)
    ├── ProjectsTab
    │   ├── WorkspaceSelector (modal)
    │   ├── ProjectList
    │   └── ProjectDetail
    │       ├── Overview
    │       ├── Issues
    │       ├── Board (read-only, horizontal)
    │       ├── Releases
    │       ├── Environments → EnvironmentDetail → DeploymentDetail
    │       └── CICD
    └── MeTab
        ├── Profile
        ├── Settings
        └── WorkspaceMembers
```

## 7. Mobile screen notes

- **Home**: environment health of the projects the user belongs to, plus assigned issues.
- **Issue detail**: status and priority change via bottom sheet, not a dropdown.
- **Board**: horizontally scrollable, read-only in MVP; status changes happen in issue detail.
- **Environment detail**: status, version, last deployment, last 10 deployments.
- **Offline/slow network**: every screen shows a retry affordance; mutations are queued only
  where a ticket explicitly says so (not in MVP).
- Deep links: `devhub://issues/{issueKey}`, `devhub://deployments/{id}` — used by notifications.

---

## 8. Shared UI rules

- Status colors: `Backlog` grey, `Todo` slate, `InProgress` blue, `InReview` amber,
  `Done` green, `Canceled` muted red.
- Health colors: Healthy green, Degraded amber, Unhealthy red, Unknown grey.
  **Never rely on color alone** — pair with a glyph (✓ ✕ ●) or label for accessibility.
- Relative timestamps ("12 min ago") with the absolute UTC value in the title/tooltip.
- Destructive actions require confirmation and name the object being destroyed.
