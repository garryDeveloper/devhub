# DEVHUB-001 — Initialize monorepo structure

|  |  |
|---|---|
| **Epic** | EPIC 1 — Foundation & project setup |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | — |
| **Specs** | [`cicd-architecture.md`](../../tech-specs/cicd-architecture.md), [`local-development.md`](../../tech-specs/local-development.md) |

## Context

Everything starts here. A monorepo keeps backend, web and mobile in one history so a change that
crosses layers is one PR and one ticket. This ticket only creates the skeleton and the hygiene
files — no application code yet.

## Scope

**In:** git repository, folder skeleton, `.gitignore`, `.editorconfig`, README, license, branch
protection, commit convention.
**Out:** any project scaffolding (DEVHUB-002, 008, 009), any workflow file (DEVHUB-012).

## Tasks

- [ ] Create the GitHub repository `devhub` (private is fine) and clone it.
- [ ] Create the folder skeleton: `api/`, `web/`, `mobile/`, `infrastructure/`, `docs/`,
      `.github/`.
- [ ] Copy this `docs/` tree into the repository as-is.
- [ ] Add a root `.gitignore` covering .NET (`bin/`, `obj/`, `*.user`), Node (`node_modules/`,
      `dist/`, `.expo/`), env files (`.env*`, `!.env.example`), OS files, and IDE folders.
- [ ] Add `.editorconfig`: UTF-8, LF, final newline, 4 spaces for C#, 2 for TS/JSON/YAML,
      trim trailing whitespace.
- [ ] Add a root `README.md`: what DevHub is, the stack table, how to run it (link to
      `docs/tech-specs/local-development.md`), and a link to `docs/README.md`.
- [ ] Add `CLAUDE.md` at the root (already written — keep it updated as the project evolves).
- [ ] Protect `main`: require a PR, require status checks (added in DEVHUB-012), no force push.
- [ ] Make the first commit on a branch and merge it through a PR, to prove the flow works.

## Acceptance criteria

- [ ] `git clone` gives a working tree with `api/`, `web/`, `mobile/`, `infrastructure/`, `docs/`.
- [ ] `git status` is clean after a build in any subproject (nothing built is tracked).
- [ ] `main` cannot be pushed to directly.
- [ ] The README explains the project in under a minute of reading.

## Technical notes

- Branch names: `feat/DEVHUB-001-short-description`. Commits follow Conventional Commits and end
  with the ticket id: `chore(repo): initialize monorepo structure (DEVHUB-001)`.
- Do not add a root `package.json` or a workspace manager yet. `web` and `mobile` are independent
  npm projects; a shared workspace is a decision to make later, if ever.
- Add `.gitattributes` with `* text=eol=lf` to avoid CRLF noise if you ever work from Windows.

## Learning goals

Repository hygiene, why a monorepo suits a single-developer full-stack project, branch
protection and PR-based flow even when working alone.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1.
