# DEVHUB-094 — VPC, subnets and security groups

|  |  |
|---|---|
| **Epic** | EPIC 16 — AWS infrastructure |
| **Phase** | 3 — AWS |
| **Priority** | P1 |
| **Size** | M |
| **Depends on** | DEVHUB-089 |
| **Specs** | [`aws-architecture.md`](../../tech-specs/aws-architecture.md) §2 |

## Context

The network comes first: RDS needs a subnet group, and the security-group chain is what actually
keeps the database private. Build it deliberately rather than accepting the default VPC.

## Scope

**In:** VPC, four subnets across two AZs, internet gateway, route tables, three security groups.
**Out:** NAT gateway, VPC endpoints, private application subnets (documented as the later
evolution).

## Tasks

- [ ] Create VPC `devhub-vpc` (10.0.0.0/16) with DNS hostnames and resolution enabled.
- [ ] Two public subnets (10.0.1.0/24, 10.0.2.0/24) and two private (10.0.11.0/24, 10.0.12.0/24)
      in different AZs.
- [ ] Internet gateway attached; a public route table with `0.0.0.0/0 → IGW` associated to the
      public subnets; the private route table with no internet route.
- [ ] Security groups: `sg-web` (443/80 from anywhere), `sg-app` (from `sg-web`), `sg-db`
      (5432 **from `sg-app` only**, referenced as a source security group, not a CIDR).
- [ ] Tag everything `Project=DevHub, Environment=staging|prod`.
- [ ] Document every console step or the equivalent CLI commands in the PR, plus teardown order.

## Acceptance criteria

- [ ] Two AZs are covered (an RDS subnet group requires it).
- [ ] Nothing in the private subnets can reach the internet, and nothing on the internet can
      reach them.
- [ ] `sg-db` allows 5432 from `sg-app` and from nothing else — verified by inspection.
- [ ] The full teardown sequence is written down.

## Technical notes

Deliberately no NAT gateway: ~$32/month for a learning project is the single easiest AWS bill
mistake to make. The application therefore lives in a public subnet for now, and
[`aws-architecture.md`](../../tech-specs/aws-architecture.md) §7 records that as a decision with
its trade-off.

## Learning goals

VPC/subnet/route-table mechanics, public vs private subnets, security groups referencing security
groups, cost-aware architecture.

## Done

Meets [`definition-of-done.md`](../../definition-of-done.md) §1, §5.
