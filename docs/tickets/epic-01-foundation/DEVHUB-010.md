# DEVHUB-010 — Linting, formatting and code style across the monorepo

|  |  |
|---|---|
| **Epic** | EPIC 1 — Foundation & project setup |
| **Phase** | 1 — Local foundation |
| **Priority** | P1 |
| **Size** | S |
| **Depends on** | DEVHUB-002, DEVHUB-008, DEVHUB-009 |
| **Specs** | `CLAUDE.md` §5 |

## Context

Consistency removes a whole category of review comments and diff noise. Automate it so it is
never a discussion.

## Scope

**In:** ESLint + Prettier for web and mobile, .NET analyzers + `dotnet format`, editorconfig
rules, optional pre-commit hook.
**Out:** CI enforcement (DEVHUB-012 wires these commands into the workflows).

## Tasks

- [ ] Web + mobile: ESLint (typescript-eslint, react-hooks, import order) and Prettier; add
      `lint`, `lint:fix`, `format`, `typecheck` scripts.
- [ ] Ban `any` and `@ts-ignore` (`@typescript-eslint/no-explicit-any: error`).
- [ ] Backend: enable .NET analyzers, set `EnforceCodeStyleInBuild`, verify `dotnet format
      --verify-no-changes` passes.
- [ ] Align `.editorconfig` with both toolchains so the IDE and CLI agree.
- [ ] Optional: `husky` + `lint-staged` on the client projects for a pre-commit format.
- [ ] Fix everything the tools report; commit the formatting change on its own.

## Acceptance criteria

- [ ] `npm run lint` passes in `web/` and `mobile/` with zero warnings.
- [ ] `dotnet format --verify-no-changes` passes in `api/`.
- [ ] A deliberately badly formatted file is flagged (or auto-fixed) before commit.

## Technical notes

- Do the mass reformat in a dedicated commit; mixing it into a feature PR destroys the diff.
- Keep the rule set small and boring. Rules you disable one by one in code are rules you should
  not have enabled.

## Learning goals

Toolchain configuration, why formatting is automated rather than agreed, keeping a diff reviewable.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1.
