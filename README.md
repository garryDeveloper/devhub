# DevHub

> Developer project & deployment workspace — issue tracking connected to environments, releases,
> deployments and CI/CD activity.

DevHub answers one question on a single screen: **what is being worked on, what was shipped, and
is production healthy?**

## Stack

| Area | Technology |
|---|---|
| Web | React + TypeScript + Vite |
| Mobile | React Native + TypeScript (Expo) |
| Backend | ASP.NET Core 10 (modular monolith) |
| Database | PostgreSQL (local Docker · AWS RDS) |
| Storage | AWS S3 (presigned uploads) |
| Compute | AWS Elastic Beanstalk |
| CDN | CloudFront |
| CI/CD | GitHub Actions + OIDC |
| Observability | CloudWatch |

## Local database

PostgreSQL 16 and Adminer, defined once in `infrastructure/docker-compose.yml`. One command on a
fresh clone, no setup step:

```bash
docker compose -f infrastructure/docker-compose.yml up -d          # start
docker compose -f infrastructure/docker-compose.yml ps             # health status
docker compose -f infrastructure/docker-compose.yml logs -f postgres
docker compose -f infrastructure/docker-compose.yml down           # stop, keep data
docker compose -f infrastructure/docker-compose.yml down -v        # stop, WIPE data
```

| | |
|---|---|
| PostgreSQL | `localhost:5432`, database/user/password all `devhub` |
| Adminer | <http://localhost:5050> — server is pre-filled, log in with `devhub` / `devhub` |

Both are bound to `127.0.0.1`, so nothing on your network can reach them. The credentials are
trivial on purpose and are not secrets; never reuse them anywhere else.

Data lives in the named volume `devhub_pgdata` and survives `down`. Only `down -v` deletes it.

If port 5432 or 5050 is already in use on your machine, copy `infrastructure/.env.example` to
`infrastructure/.env` and set `POSTGRES_PORT` / `ADMINER_PORT`. That file is git-ignored, so a
fresh clone still gets the standard ports — see
[`local-development.md`](docs/tech-specs/local-development.md) §3.

## Running the API in Docker

Elastic Beanstalk runs the API as a container, so the image is the deployment artifact — not a
convenience. Building it locally is how "works on my machine" stops being a deployment problem.

```bash
docker build -t devhub-api:local api

# Joins the compose network from "Local database" above, so the API reaches PostgreSQL by
# its service name. Start that stack first.
docker run --rm -p 5080:8080 --network devhub_default \
  -e Cors__AllowedOrigins__0=http://localhost:5173 \
  -e ConnectionStrings__Default="Host=postgres;Port=5432;Database=devhub;Username=devhub;Password=devhub" \
  devhub-api:local

curl http://localhost:5080/health
# {"status":"Healthy","checks":{},"version":"1.0.0","durationMs":0}
```

The container listens on **8080**, not 80: ports below 1024 need root or `CAP_NET_BIND_SERVICE`,
and the image runs as the non-root user `app` (uid 1654). Port 5080 on the host keeps the same
address the API has when you run it with `dotnet run`.

Both environment variables are required, and both fail at **startup** rather than on the first
request that needs them: an empty CORS allow-list (DEVHUB-003) and a missing connection string
(DEVHUB-006) each abort the boot with a named message. A container with no
`ASPNETCORE_ENVIRONMENT` runs as Production, which is where those guards apply. The image bakes
in no configuration and no secrets — `__` is the .NET convention for nesting, so
`Cors__AllowedOrigins__0` is `Cors:AllowedOrigins[0]`.

Inside the compose network the host is `postgres` on port 5432. From outside it is `localhost`
on whatever host port you published (5432 by default).

Swagger is not served from this image unless you pass `-e ASPNETCORE_ENVIRONMENT=Development`.

## Repository

```text
api/            ASP.NET Core backend         (created by DEVHUB-002)
web/            React web app                (created by DEVHUB-008)
mobile/         React Native app             (created by DEVHUB-009)
infrastructure/ docker-compose, IaC          (created by DEVHUB-005)
.github/        GitHub Actions workflows     (created by DEVHUB-012)
docs/           specifications + backlog
CLAUDE.md       working agreement for AI assistants in this repo
```

## Getting started

1. Read [`docs/README.md`](docs/README.md) — the documentation index.
2. Follow [`docs/roadmap.md`](docs/roadmap.md) for what to build in what order.
3. Pick your first ticket from [`docs/tickets/README.md`](docs/tickets/README.md).
4. Set up your machine with
   [`docs/tech-specs/local-development.md`](docs/tech-specs/local-development.md).

## Project intent

DevHub is a **learning project first, portfolio project second**: the point is to understand
AWS, .NET and React well enough to explain every decision in it. See `CLAUDE.md` §7 for how that
shapes the way work is done here.

## License

[MIT](LICENSE)
