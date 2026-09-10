# DevHub documentation

Everything needed to implement DevHub. Read in this order the first time.

## 1. Start here

| Doc | What it answers |
|---|---|
| [`product-spec.md`](product-spec.md) | The original product & technical specification. The "why". |
| [`roadmap.md`](roadmap.md) | What to build, in what order, in 5 phases. |
| [`glossary.md`](glossary.md) | Shared vocabulary. Read before naming anything. |
| [`definition-of-done.md`](definition-of-done.md) | When a ticket is actually finished. |

## 2. Design

| Doc | What it answers |
|---|---|
| [`domain-model.md`](domain-model.md) | Entities, aggregates, invariants, domain events. |
| [`screens-and-navigation.md`](screens-and-navigation.md) | Web routes, mobile navigation, screen inventory. |
| [`user-flows.md`](user-flows.md) | The nine end-to-end flows the product must support. |

## 3. Technical specifications

| Doc | What it answers |
|---|---|
| [`tech-specs/backend-architecture.md`](tech-specs/backend-architecture.md) | Layers, modules, request flow, domain events, CQRS-lite. |
| [`tech-specs/frontend-web-architecture.md`](tech-specs/frontend-web-architecture.md) | React structure, state, data fetching, routing, forms. |
| [`tech-specs/mobile-architecture.md`](tech-specs/mobile-architecture.md) | React Native structure, navigation, secure storage, offline behaviour. |
| [`tech-specs/api-conventions.md`](tech-specs/api-conventions.md) | REST semantics, status codes, errors, pagination, versioning. |
| [`tech-specs/api-endpoints.md`](tech-specs/api-endpoints.md) | Every endpoint with request/response contracts. |
| [`tech-specs/database-schema.md`](tech-specs/database-schema.md) | Tables, columns, indexes, constraints, migration rules. |
| [`tech-specs/auth-spec.md`](tech-specs/auth-spec.md) | JWT, refresh-token rotation, authorization model. |
| [`tech-specs/search-spec.md`](tech-specs/search-spec.md) | PostgreSQL full-text search design. |
| [`tech-specs/webhooks-spec.md`](tech-specs/webhooks-spec.md) | Inbound webhook contracts, signing, idempotency. |
| [`tech-specs/storage-s3-spec.md`](tech-specs/storage-s3-spec.md) | S3 layout, presigned uploads, lifecycle, IAM. |
| [`tech-specs/observability-spec.md`](tech-specs/observability-spec.md) | Logging, correlation ids, metrics, alarms, health checks. |
| [`tech-specs/testing-strategy.md`](tech-specs/testing-strategy.md) | What to test where, and with what. |
| [`tech-specs/local-development.md`](tech-specs/local-development.md) | Running the whole stack on your machine. |
| [`tech-specs/aws-architecture.md`](tech-specs/aws-architecture.md) | AWS topology, services, IAM plan, networking evolution. |
| [`tech-specs/cicd-architecture.md`](tech-specs/cicd-architecture.md) | GitHub Actions workflows, OIDC, environments, promotion. |

## 4. Backlog

[`tickets/README.md`](tickets/README.md) — 112 tickets across 18 epics, grouped by epic folder,
each with scope, acceptance criteria, technical notes and dependencies.

---

## How to use this documentation

1. Pick the next ticket from [`tickets/README.md`](tickets/README.md), following
   [`roadmap.md`](roadmap.md).
2. Read the specs the ticket references.
3. Design first, implement second (see `CLAUDE.md` §7).
4. Check the ticket against [`definition-of-done.md`](definition-of-done.md).
5. If the implementation diverges from a spec, **update the spec in the same PR**. These
   documents are the source of truth; drift is a bug.
