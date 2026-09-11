using DevHub.Application;
using DevHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// The composition root. Each layer registers its own services; Program.cs only wires them
// together, so this file should stay this short as the application grows.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Placeholder until DEVHUB-003 adds /health and Swagger.
app.MapGet("/", () => Results.Ok(new { service = "DevHub.Api", status = "ok" }));

app.Run();

/// <summary>Exposed so DEVHUB-011's integration tests can use WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program;
