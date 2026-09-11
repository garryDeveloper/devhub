# CI/CD architecture

GitHub Actions builds, tests and deploys DevHub — and then tells DevHub about it, which is the
feature the product is built around.

---

## 1. Repository shape

Monorepo. One repository, one issue tracker, one PR per change even when it spans backend and
frontend, and one place for the workflows.

```text
devhub/
├── api/                    DevHub.sln
├── web/
├── mobile/
├── infrastructure/         docker-compose, deployment assets
├── docs/
└── .github/workflows/
```

Path filters keep the monorepo cheap: a docs-only PR must not run integration tests.

---

## 2. Workflows

```text
.github/workflows/
├── backend-ci.yml          PR + push: build, test, integration tests   (DEVHUB-101)
├── web-ci.yml              PR + push: typecheck, lint, test, build     (DEVHUB-102)
├── mobile-ci.yml           PR + push: typecheck, lint, test            (DEVHUB-103)
├── deploy-staging.yml      push to main: build, deploy, health, notify (DEVHUB-105)
└── deploy-production.yml   manual dispatch + approval                  (DEVHUB-106)
```

---

## 3. Pull request pipeline

```text
Pull request
    ├── backend-ci   (paths: api/**)
    │     restore → build → architecture tests → unit tests → integration tests (Testcontainers)
    ├── web-ci       (paths: web/**)
    │     install → typecheck → lint → unit tests → build
    └── mobile-ci    (paths: mobile/**)
          install → typecheck → lint → unit tests
```

Rules:

- All three are **required status checks** on `main`; the branch is protected and requires a PR.
- Concurrency group per branch with `cancel-in-progress: true` — a new push cancels the old run.
- Caching: `actions/setup-dotnet` + NuGet cache, `actions/setup-node` with `cache: npm`.
- Target: under 8 minutes for the slowest job.
- A failing test **fails the workflow**. No `continue-on-error` on test steps, ever.

---

## 4. Main branch pipeline

```text
push to main
    ↓
backend-ci + web-ci + mobile-ci        (must pass)
    ↓
deploy-staging.yml
    ├── compute version   (1.<run_number>.0, or a tag)
    ├── docker build + push API image / package the Beanstalk bundle
    ├── build the web SPA with the staging API URL
    ├── configure AWS credentials via OIDC (assume DevHubGitHubActionsRole)
    ├── notify DevHub: status = Running
    ├── deploy API to devhub-api-staging
    ├── sync the SPA to s3://devhub-web-staging + CloudFront invalidation
    ├── health check: poll /health/ready until 200 (max 5 min)
    └── notify DevHub: status = Succeeded | Failed        (always runs — if: always())
```

The health check is the gate: a deployment that answers `200` on `/health/ready` is "deployed";
anything else fails the job and reports `Failed`.

---

## 5. Production pipeline

Manual on purpose in the MVP. Automating a production deploy before you have watched a rollback
teaches the wrong lesson.

```text
workflow_dispatch (input: version to promote)
    ↓
verify the version exists as a staging-tested artifact   ← promote the artifact, never rebuild
    ↓
environment: production   → GitHub required reviewer → you approve in the UI
    ↓
notify DevHub: Running (Production)
    ↓
deploy to devhub-api-production
    ↓
health check /health/ready
    ↓
notify DevHub: Succeeded | Failed
    ↓
on failure: `eb deploy --version <previous>` (documented, manual in the MVP)
```

**Promote the artifact that staging tested.** Rebuilding for production means production runs
bytes nothing ever tested.

---

## 6. GitHub → AWS with OIDC (DEVHUB-104)

No AWS access keys in GitHub. Ever.

```text
1. Create an IAM OIDC identity provider for token.actions.githubusercontent.com
2. Create DevHubGitHubActionsRole with this trust policy condition:
      "token.actions.githubusercontent.com:sub":
          "repo:<owner>/devhub:ref:refs/heads/main"
          "repo:<owner>/devhub:environment:production"
3. Attach a least-privilege deploy policy (see aws-architecture.md §3)
4. In the workflow:
      permissions: { id-token: write, contents: read }
      - uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: arn:aws:iam::<acct>:role/DevHubGitHubActionsRole
          aws-region: us-east-1
```

The `sub` condition is the security boundary: without it, **any** repository could assume the
role. Restrict to your repo and to the specific branch/environment.

Verify the restriction works by trying to use the role from a feature branch — it must fail.

---

## 7. Versioning & artifacts

```text
Version:      1.<github.run_number>.0        (semver-ish, monotonic, traceable to a run)
Docker tag:   devhub-api:1.42.0 and :sha-8a2d91f
Beanstalk:    application version label = the same string
Web build:    content-hashed assets + a version stamp in index.html
```

Every artifact carries `commitSha` and `runId` as labels/metadata, so a deployed version can
always be traced back to a commit and a workflow run — and that is exactly what the DevHub
deployment detail screen shows.

---

## 8. Reporting back into DevHub (DEVHUB-107)

The step that closes the loop:

```yaml
- name: Notify DevHub
  if: always()
  run: ./.github/scripts/notify-devhub.sh
  env:
    DEVHUB_API:    ${{ vars.DEVHUB_API_URL }}
    DEVHUB_SECRET: ${{ secrets.DEVHUB_WEBHOOK_SECRET }}
    DEVHUB_PROJECT:${{ vars.DEVHUB_PROJECT_ID }}
    STATUS:        ${{ job.status }}
```

Requirements:

- `if: always()` — a failed deployment is the more interesting event and must still report.
- The script signs the body per [`webhooks-spec.md`](webhooks-spec.md).
- A callback failure logs a warning and does **not** fail the deployment: DevHub's reporting must
  never break the deploy it is reporting on.
- Send `Running` before the deploy and the terminal status after, so the timeline is real.

Additionally, `workflow_run` events from GitHub feed `POST /api/webhooks/github/actions`
(DEVHUB-078), which populates the CI/CD screen for *all* workflows, not just deploys.

---

## 9. Secrets & variables

| Kind | Name | Where |
|---|---|---|
| secret | `DEVHUB_WEBHOOK_SECRET` | repository secret |
| variable | `DEVHUB_API_URL`, `DEVHUB_PROJECT_ID` | environment variables per GitHub environment |
| — | AWS credentials | **none** — OIDC assumed role |

GitHub environments (`staging`, `production`) hold environment-scoped variables and the
production required-reviewer rule. Secrets are never echoed; `set -x` is never used in a script
that touches them.

---

## 10. Branch strategy

```text
main              always deployable, protected, deploys to staging
feat/DEVHUB-042-* short-lived branches, one ticket each
```

Squash merge, PR title `feat(issues): add status endpoint (DEVHUB-042)`. No long-lived `develop`
branch — with one developer it only creates merge debt.

---

## 11. Failure handling

| Failure | Behaviour |
|---|---|
| Unit/integration test fails | workflow fails, no deploy, PR blocked |
| Docker build fails | workflow fails, previous version keeps running |
| Beanstalk deploy fails | Beanstalk rolls back the batch; job reports `Failed` to DevHub |
| Health check fails | job fails, reports `Failed`; manual rollback documented in the ticket |
| DevHub callback fails | warning only; the deploy result stands |
| Production approval not granted | job waits, then times out; nothing changes |

---

## 12. Post-MVP

- Dependabot + an automated dependency-update PR lane.
- CodeQL / `dotnet list package --vulnerable` in CI.
- Preview environments per PR (costly on Beanstalk — a reason to look at ECS/Fargate).
- Blue/green deployments with Beanstalk environment URL swap.
- Automated rollback when the post-deploy error rate alarm fires.
- Terraform/CDK for the infrastructure the console currently owns.
