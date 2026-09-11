# DEVHUB-033 — Web project list and creation

|  |  |
|---|---|
| **Epic** | EPIC 4 — Projects |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-027, DEVHUB-031 |
| **Specs** | [`screens-and-navigation.md`](../../screens-and-navigation.md) §4 |

## Context

The project list is the entry point to everything the user actually works on, and the create
form is where the key convention is explained.

## Scope

**In:** project list screen, create modal with key suggestion, sidebar project navigation.
**Out:** project settings (DEVHUB-034), dashboard (DEVHUB-083).

## Tasks

- [ ] `/w/:slug/projects`: card grid showing color, key badge, name, open-issue count.
- [ ] "Archived" toggle that adds `includeArchived=true` to the query.
- [ ] Create modal: name, auto-suggested key (uppercase initials, editable), description, color
      picker; explain that the key cannot change later.
- [ ] Live client-side key validation matching the server regex, plus mapping the server's `409`
      onto the key field.
- [ ] Add projects to the sidebar with their sub-navigation.
- [ ] Empty state ("No projects yet") with the create action, plus loading and error states.

## Acceptance criteria

- [ ] Creating a project navigates straight into it.
- [ ] A duplicate key shows the error on the key field, not as a toast.
- [ ] The archived toggle works and archived cards are visually distinct.
- [ ] The list renders correctly at 768px.

## Technical notes

Suggest the key from the name (`DevHub API` → `DEV`) but let the user override it — and validate
on the client with the *same* regex the server uses, so the two never disagree.

## Learning goals

Form UX for immutable fields, mapping server conflicts to form fields, list/grid states.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §3.
