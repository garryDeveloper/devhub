# AWS architecture

The cloud target, and the order in which to learn it. Every service here exists in the project
because a feature needs it — not to collect logos.

---

## 1. Initial architecture (the one to build)

```text
                              Internet
                                 │
                  ┌──────────────┴───────────────┐
                  ▼                              ▼
            CloudFront                    Elastic Beanstalk
          (SPA distribution)              (ASP.NET Core API)
                  │                              │
                  ▼                    ┌─────────┴──────────┐
             S3 (web bucket)           ▼                    ▼
             private + OAC        RDS PostgreSQL       S3 (attachments)
                                  private subnet        private bucket
                                       │                    │
                                       └────────┬───────────┘
                                                ▼
                                           CloudWatch
                                         logs · metrics · alarms

                                              IAM
                    ┌──────────────┬───────────┴────────┬──────────────┐
                    ▼              ▼                    ▼              ▼
             BeanstalkEC2Role  GitHubActionsRole   LambdaRole    (developer user)
```

Chosen deliberately: **Elastic Beanstalk before ECS**. Beanstalk teaches environments, health,
rolling deploys and instance roles without also teaching task definitions, services and load
balancer target groups on day one. ECS/Fargate is a documented later evolution, not a v1
requirement.

---

## 2. Networking

### Learning topology (start here — DEVHUB-094)

```text
VPC 10.0.0.0/16
├── Public subnet  10.0.1.0/24  (AZ a)   → Elastic Beanstalk instance, Internet Gateway
├── Public subnet  10.0.2.0/24  (AZ b)
├── Private subnet 10.0.11.0/24 (AZ a)   → RDS
└── Private subnet 10.0.12.0/24 (AZ b)   → RDS (required: subnet group needs 2 AZs)
```

Security groups (the important part):

```text
sg-web       inbound  443/80 from 0.0.0.0/0          → Beanstalk / load balancer
sg-app       inbound  from sg-web only               → application instances
sg-db        inbound  5432 from sg-app ONLY          → RDS
```

`sg-db` referencing `sg-app` **as a source security group**, not a CIDR, is the lesson: the
database is reachable only from the application, and it keeps working when instances are
replaced.

No NAT Gateway in the learning topology — it costs ~$32/month and is not needed while the app
sits in a public subnet. When the app moves to a private subnet, a NAT (or VPC endpoints for S3
and SSM) becomes necessary; budget for it before making that change.

### Production-shaped evolution (later)

```text
Internet → CloudFront → S3 (SPA)
Internet → ALB (public subnets) → application (private subnets) → RDS (private subnets)
                                              └→ VPC endpoints: S3, SSM, CloudWatch Logs
```

---

## 3. Services, and what each one teaches

### IAM (DEVHUB-099)

Roles to create — one per responsibility, never one shared role:

```text
DevHubGitHubActionsRole
  trust: GitHub OIDC provider, condition on repo + branch/environment
  allow: elasticbeanstalk:CreateApplicationVersion/UpdateEnvironment,
         s3:PutObject on the artifacts + web bucket, cloudfront:CreateInvalidation
  deny:  everything else. No access to the attachments bucket. No iam:*

DevHubApplicationRole (EC2 instance profile)
  allow: s3 Get/Put/Delete/Head on devhub-attachments-{env}/*
         ssm:GetParameters on /devhub/{env}/*
         kms:Decrypt on the parameter key
         logs:CreateLogStream, logs:PutLogEvents on /devhub/api/{env}
  deny:  everything else

DevHubLambdaRole (post-MVP)
  allow: sqs:ReceiveMessage/DeleteMessage on the queue, logs:*, s3 on a specific prefix
```

Rules: never `AdministratorAccess` for a workload; never an IAM **user** with long-lived keys for
CI (that is the whole point of OIDC); scope every `Resource` to an ARN; use conditions
(`aws:SourceArn`, repo/branch on the OIDC trust) so a leaked role cannot be assumed from
anywhere.

**Learn:** users vs roles vs policies, trust policies vs permission policies, service roles,
instance profiles, policy evaluation (explicit deny wins), least privilege in practice.

### S3 (DEVHUB-089, 098)

Two buckets, both private (see [`storage-s3-spec.md`](storage-s3-spec.md)). The web bucket is
reachable only through CloudFront using **Origin Access Control**; the attachments bucket only
through presigned URLs.

**Learn:** buckets vs objects vs keys, bucket policy vs IAM policy vs Block Public Access
precedence, presigned URLs, versioning, lifecycle rules, SSE, CORS.

### RDS PostgreSQL (DEVHUB-095)

```text
Engine        PostgreSQL 16
Class         db.t4g.micro (free-tier eligible)
Storage       20 GB gp3, autoscaling off
Multi-AZ      no (cost; document the availability trade-off)
Public access NO — this is the single most important setting
Subnet group  the two private subnets
Backups       7 days, automated
Encryption    on
Parameters    log_min_duration_statement = 500ms
```

Connection string comes from SSM Parameter Store, never from `appsettings.json`. Connect from
your machine through an SSM session/bastion, not by making the instance public "just for now" —
that "temporarily" is how databases end up on the internet.

**Learn:** subnet groups, security groups, parameter groups, automated backups vs snapshots,
connection limits and pooling, `Maximum Pool Size` in Npgsql.

### Elastic Beanstalk (DEVHUB-096)

```text
Platform       Docker running on 64bit Amazon Linux 2023
Environment    devhub-api-staging, devhub-api-production
Type           single instance (staging) / load-balanced (production, later)
Instance       t3.micro
Health         enhanced, path /health/live
Deploy policy  rolling with additional batch
Env vars       injected from SSM at startup (DEVHUB-097)
```

**Learn:** application vs environment vs version, the deploy lifecycle, enhanced health
reporting, `.ebextensions`, instance profile vs service role, log streaming to CloudWatch,
rollback to a previous version.

### CloudFront (DEVHUB-098)

SPA distribution over the web bucket with OAC, default root object `index.html`, and a custom
error response mapping `403/404 → /index.html` with status `200` — without that, deep links like
`/p/DEV/issues` return an S3 error instead of the app. Invalidate `/*` on deploy (or use
content-hashed filenames and invalidate only `index.html`).

**Learn:** origins, behaviours, cache policies, OAC vs the legacy OAI, invalidations, TTLs.

### CloudWatch (DEVHUB-100, 111)

Log groups, retention, metric filters, alarms, one dashboard. Details in
[`observability-spec.md`](observability-spec.md).

### SQS + Lambda (post-MVP)

```text
GitHub webhook → API → persist event → SQS → Lambda → process → update deployment → notify
S3 upload      → S3 event            → SQS → Lambda → thumbnail → S3
```

Introduce only when there is a real reason (slow webhook processing, image work). Async
infrastructure everywhere is a cost, not a badge.

---

## 4. Configuration & secrets (DEVHUB-097)

```text
/devhub/{env}/ConnectionStrings__Default     SecureString
/devhub/{env}/Jwt__Secret                    SecureString
/devhub/{env}/Webhooks__DefaultSecret        SecureString
/devhub/{env}/Storage__BucketName            String
/devhub/{env}/Cors__AllowedOrigins           String
```

SSM Parameter Store (free tier) over Secrets Manager (~$0.40/secret/month) for this project.
The instance role can read only its own environment's path. Parameters are injected as
environment variables at container start; the double underscore maps to .NET's configuration
hierarchy (`ConnectionStrings:Default`).

**No secret is ever committed, printed in a log, or passed on a command line.**

---

## 5. Environments

| | Staging | Production |
|---|---|---|
| Beanstalk env | `devhub-api-staging` | `devhub-api-production` |
| RDS | shared instance, separate database | own instance |
| Buckets | `devhub-attachments-staging` | `devhub-attachments-prod` |
| Deploy | automatic on `main` | manual approval |
| Log retention | 7 days | 30 days |
| Swagger | enabled | disabled |

---

## 6. Cost control

| Resource | Approx. monthly |
|---|---|
| RDS db.t4g.micro + 20 GB | free tier for 12 months, then ~$15 |
| EC2 t3.micro (Beanstalk) | free tier, then ~$8 |
| S3 + CloudFront (low traffic) | < $1 |
| CloudWatch logs (30 d, low volume) | < $2 |
| **NAT Gateway** | **~$32 — avoid until genuinely needed** |

Practices: a billing alarm at $10 **before** creating anything else; tag every resource
`Project=DevHub, Environment={env}`; stop or delete the staging environment when not in use;
delete unattached EBS volumes and old snapshots; keep log retention short.

Every infrastructure ticket records its **teardown** steps. Being able to delete it cleanly is
part of understanding it.

---

## 7. Deliberate simplifications

Documented so they read as decisions, not gaps:

| Simplification | Why | When to revisit |
|---|---|---|
| Single AZ, no Multi-AZ RDS | cost; this is not a production SLA | if it ever hosts real users |
| App in a public subnet | avoids NAT cost, easier to debug | when moving to ALB + private subnets |
| Elastic Beanstalk, not ECS | fewer new concepts at once | when container orchestration is the thing being learned |
| Console-first, IaC later | see the resources before abstracting them | after the architecture stops changing weekly |
| SSM Parameter Store, not Secrets Manager | free, sufficient | if automatic rotation is needed |
| No WAF, no Shield Advanced | no threat model, no budget | if the app is publicly promoted |

IaC (Terraform or CDK) is a **post-MVP** ticket on purpose: writing Terraform for resources you
do not yet understand teaches Terraform, not AWS. Build it by hand once, document every step,
then codify it.
