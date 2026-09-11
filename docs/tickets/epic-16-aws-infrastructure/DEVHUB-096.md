# DEVHUB-096 — Elastic Beanstalk environment for the API

|  |  |
|---|---|
| **Epic** | EPIC 16 — AWS infrastructure |
| **Phase** | 3 — AWS |
| **Priority** | P1 |
| **Size** | L |
| **Depends on** | DEVHUB-095, DEVHUB-004 |
| **Specs** | [`aws-architecture.md`](../../tech-specs/aws-architecture.md) §3 |

## Context

The API's home in AWS. Beanstalk is chosen over ECS to keep the number of new concepts
manageable — see the decision table in the AWS spec.

## Scope

**In:** Beanstalk application and staging environment running the Docker image, instance profile,
health monitoring, log streaming, a first manual deploy.
**Out:** production environment (DEVHUB-106), automated deploys (DEVHUB-105).

## Tasks

- [ ] Create the Beanstalk application `devhub-api` and environment `devhub-api-staging`
      (Docker on AL2023, single instance, `t3.micro`, in a public subnet of the VPC).
- [ ] Attach `DevHubApplicationRole` as the instance profile (S3, SSM, CloudWatch Logs only).
- [ ] Configure enhanced health with `/health/live` as the health-check path.
- [ ] Enable log streaming to CloudWatch.
- [ ] Set environment variables (temporarily by hand; SSM injection lands in DEVHUB-097).
- [ ] Deploy manually with `eb deploy` or a zipped `Dockerrun` bundle, and verify `/health/ready`.
- [ ] Practise a rollback to the previous application version.
- [ ] Document the whole procedure and the teardown.

## Acceptance criteria

- [ ] The API is reachable over the environment URL and `/health/ready` returns `Healthy`.
- [ ] The instance connects to RDS through `sg-app` → `sg-db`.
- [ ] Application logs appear in CloudWatch.
- [ ] A rollback to the previous version works and is documented.

## Technical notes

The instance profile is what makes the S3 code work with no credentials in configuration. If
you find yourself adding an access key to make it work, the role is wrong — fix the role.

## Learning goals

Beanstalk application/environment/version model, instance profiles, enhanced health, deployment
and rollback, log streaming.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §5.
