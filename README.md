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
| Backend | ASP.NET Core 8 (modular monolith) |
| Database | PostgreSQL (local Docker · AWS RDS) |
| Storage | AWS S3 (presigned uploads) |
| Compute | AWS Elastic Beanstalk |
| CDN | CloudFront |
| CI/CD | GitHub Actions + OIDC |
| Observability | CloudWatch |

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
