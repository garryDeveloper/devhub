# Observability specification

Logging, correlation, metrics, alarms and health checks. The goal is to be able to answer
"what happened to *that* request?" from CloudWatch alone.

---

## 1. Structured logging

Serilog with the JSON formatter. **Never** `Console.WriteLine`, never string-concatenated
messages.

```csharp
_logger.LogInformation("Issue status changed {IssueKey} {OldStatus} {NewStatus} {ActorId}",
    issue.Key, oldStatus, newStatus, actorId);
```

The message template stays constant so log lines are groupable; values become structured fields.

Enrichers on every line: `CorrelationId`, `UserId` (when authenticated), `Environment`,
`Version`, `MachineName`, `RequestPath`, `SourceContext`.

| Level | Use |
|---|---|
| `Debug` | local only; noisy detail, ignored events |
| `Information` | request completed, business state change, webhook processed |
| `Warning` | handled failure: validation rejected, auth failed, retry, degraded dependency |
| `Error` | unhandled exception, failed dependency call, failed background job |
| `Fatal` | the process cannot continue (startup failure) |

Production minimum level: `Information`, with `Microsoft.*` and `Microsoft.EntityFrameworkCore.*`
at `Warning` — EF's `Information` level logs every SQL statement and will bury everything else
(and can leak parameter values).

### Never log

Passwords, password hashes, access or refresh tokens, webhook secrets, presigned URLs (they are
credentials), full request/response bodies, or email addresses in bulk. Log ids, not payloads.

---

## 2. Correlation id

Middleware, first in the pipeline:

1. Read `X-Correlation-Id`; if absent, generate a GUID.
2. Push it into the Serilog `LogContext` for the request scope.
3. Echo it on the response, always — success and error.
4. Put it in `ProblemDetails.traceId` so a screenshot of an error maps to a log query.
5. Forward it on outbound calls (webhook callbacks, AWS SDK where supported).

Clients generate one per user action, so a single click is traceable across web → API → logs.
`Activity.Current?.Id` (W3C trace id) is used when present so the correlation id and the trace
id agree.

---

## 3. Request logging

One summary line per request (Serilog request logging), not one per middleware:

```json
{
  "@t": "2026-09-06T14:32:11.4Z",
  "@l": "Information",
  "@mt": "HTTP {Method} {Path} responded {StatusCode} in {Elapsed:0.0} ms",
  "Method": "PATCH", "Path": "/api/issues/{id}/status",
  "StatusCode": 200, "Elapsed": 34.2,
  "CorrelationId": "…", "UserId": "…", "Environment": "production", "Version": "1.8.2"
}
```

`Path` uses the **route template**, not the concrete URL, so metrics group correctly and ids do
not end up in log dimensions. Health-check requests are logged at `Debug` — otherwise the load
balancer's polling drowns the log.

---

## 4. Exception handling

A single exception-handling middleware (or `IExceptionHandler`, available since .NET 8) maps exceptions to
`ProblemDetails`:

| Exception | Status | Logged at |
|---|---|---|
| `ValidationException` | 400 | Warning |
| `UnauthorizedException` | 401 | Warning |
| `ForbiddenException` | 403 | Warning |
| `NotFoundException` | 404 | Warning |
| `ConflictException` | 409 | Warning |
| `DomainException` | 422 | Warning |
| anything else | 500 | **Error**, with the full exception |

A `500` response body contains only `title`, `status` and `traceId`. Stack traces are for logs,
not for clients.

---

## 5. Health checks

```text
GET /health        → aggregate, human-readable JSON
GET /health/live   → process is alive; touches NO dependency
GET /health/ready  → database + storage reachable
```

```jsonc
{ "status": "Healthy",
  "checks": { "database": "Healthy", "storage": "Healthy" },
  "version": "1.8.2", "durationMs": 12 }
```

- `live` is what the load balancer polls. If it checked the database, a brief database blip
  would kill healthy instances.
- `ready` gates traffic after a deploy and is what the CI health-check step calls.
- Each check has a 3-second timeout; a slow check must not hang the endpoint.
- Health endpoints are anonymous but return no infrastructure detail (no connection strings,
  no host names).

---

## 6. Metrics

Emit as CloudWatch EMF (embedded metric format) inside log lines, or via
`System.Diagnostics.Metrics` + the OpenTelemetry CloudWatch exporter. Namespace `DevHub`.

| Metric | Type | Dimensions | Why |
|---|---|---|---|
| `http.server.duration` | histogram | route, method, status | latency, p95 |
| `http.server.requests` | counter | route, method, status | traffic, error rate |
| `db.query.duration` | histogram | operation | slow query detection |
| `auth.login.failed` | counter | reason | brute-force detection |
| `webhook.received` | counter | source, valid | integration health |
| `deployment.completed` | counter | environment, status | product metric |
| `attachment.uploaded` | counter | contentType | S3 usage |

Keep dimension cardinality low: **route templates, never concrete ids**. A `userId` dimension
would create one metric stream per user and a surprising bill.

---

## 7. CloudWatch configuration

```text
Log groups
  /devhub/api/{env}          retention 30 days (7 in staging)
  /devhub/lambda/{name}      retention 14 days
```

Metric filters turn log patterns into metrics where an SDK metric would be overkill
(e.g. count of `@l = "Error"`).

Alarms (DEVHUB-111):

| Alarm | Condition | Action |
|---|---|---|
| High error rate | 5xx > 5% of requests over 5 min | SNS → email |
| API latency | p95 > 1500 ms for 10 min | SNS → email |
| Health check failing | ready unhealthy 3 consecutive checks | SNS → email |
| Database connections | > 80% of RDS max for 5 min | SNS → email |
| Deployment failed | `deployment.completed{status=Failed}` ≥ 1 | SNS → email |
| Log errors | `@l = "Error"` count > 10 in 5 min | SNS → email |

Every alarm must be **actionable**. An alarm nobody acts on gets ignored and then the real one
gets ignored too. Test each alarm once by forcing the condition, and write down in the ticket
what you did to trigger it.

---

## 8. Dashboards

One CloudWatch dashboard, `devhub-{env}`:

```text
Row 1  Request rate · Error rate · p50/p95 latency
Row 2  RDS CPU · connections · free storage
Row 3  Beanstalk instance health · deployment events
Row 4  Recent ERROR log lines (Logs Insights widget)
```

Useful Logs Insights queries to save:

```text
fields @timestamp, @message
| filter CorrelationId = "…"
| sort @timestamp asc

fields @timestamp, Path, StatusCode, Elapsed
| filter StatusCode >= 500
| stats count() by Path
```

---

## 9. Frontend observability (DEVHUB-112)

- A global error boundary renders a recoverable error screen and logs the error.
- The API client attaches `X-Correlation-Id` and surfaces `traceId` in the error UI ("Reference:
  4bf92f…") so a bug report is traceable.
- Failed mutations show a toast with a retry, never a silent failure.
- A third-party error tracker (Sentry) is optional and post-MVP; if added, scrub tokens and PII.

---

## 10. Test checklist

- [ ] `X-Correlation-Id` is echoed and appears in the request log line.
- [ ] A 500 response contains `traceId` and no stack trace.
- [ ] `/health/live` returns 200 while the database is stopped; `/health/ready` returns 503.
- [ ] No log line in a full end-to-end run contains a token, a password or a presigned URL.
- [ ] EF Core SQL logging is not at `Information` in production configuration.
