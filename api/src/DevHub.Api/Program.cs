using DevHub.Api.Extensions;
using DevHub.Application;
using DevHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// The composition root. Each layer registers its own services; Program.cs only wires them
// together and fixes the middleware order, so this file stays readable as the app grows.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Names only, never values (DEVHUB-007) — this answers "did my override actually load?"
// without ever being able to leak a secret into the console or CloudWatch.
var configurationSources = string.Join(", ", ((IConfigurationRoot)app.Configuration).Providers);
app.Logger.LogInformation("Configuration sources loaded: {Sources}", configurationSources);

// Swagger is a development tool and an information leak in production: it publishes every
// route, DTO shape and status code the API has.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "DevHub API v1"));
}

// Outermost, so an exception anywhere below becomes a ProblemDetails 500 with a traceId — never
// a stack trace (CLAUDE.md §4). UseStatusCodePages gives bodiless responses (JwtBearer's 401)
// the same shape.
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseCors();

// Before authentication: rejecting a flood by IP is cheaper than validating its tokens first.
app.UseRateLimiter();

// After UseCors (DEVHUB-003). Put authorization first and anonymous CORS preflight requests are
// rejected before the CORS middleware ever sees them, which looks like a browser bug rather
// than a pipeline bug.
app.UseAuthentication();
app.UseAuthorization();

app.MapApiHealthChecks();
app.MapControllers();

app.Run();

/// <summary>Exposed so DEVHUB-011's integration tests can use WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program;
