# DEVHUB-004 — Dockerize the API

|  |  |
|---|---|
| **Epic** | EPIC 1 — Foundation & project setup |
| **Phase** | 1 — Local foundation |
| **Priority** | P0 |
| **Size** | S |
| **Depends on** | DEVHUB-003 |
| **Specs** | [`cicd-architecture.md`](../../tech-specs/cicd-architecture.md), [`aws-architecture.md`](../../tech-specs/aws-architecture.md) |

## Context

Elastic Beanstalk will run the API as a Docker container, and CI will build that image. Doing it
now means "works on my machine" never becomes a deployment problem later.

## Scope

**In:** multi-stage Dockerfile, `.dockerignore`, local run instructions.
**Out:** pushing to a registry (DEVHUB-105), Beanstalk configuration (DEVHUB-096).

## Tasks

- [ ] Multi-stage `api/Dockerfile`: `sdk:8.0` to restore/publish, `aspnet:8.0-alpine` to run.
- [ ] Copy `.csproj` files and restore **before** copying the source, so layer caching works.
- [ ] Run as a non-root user; expose port 8080; set `ASPNETCORE_URLS=http://+:8080`.
- [ ] Add `.dockerignore` (bin, obj, node_modules, .git, tests, docs).
- [ ] Add a `HEALTHCHECK` hitting `/health`.
- [ ] Document `docker build -t devhub-api:local api` and the `docker run` command in the README.

## Acceptance criteria

- [ ] The image builds from a clean checkout.
- [ ] `docker run -p 5080:8080 devhub-api:local` serves `/health`.
- [ ] Image size is under ~250 MB.
- [ ] Changing only a `.cs` file rebuilds without re-restoring NuGet packages.

## Technical notes

- Non-root matters: Beanstalk and ECS both run untrusted-by-default, and a root container is an
  unnecessary escalation path.
- Do not bake configuration or secrets into the image. Everything comes from environment
  variables at runtime.

## Learning goals

Multi-stage builds, Docker layer caching, why the container listens on 8080 and not 80, image
hygiene.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1.
