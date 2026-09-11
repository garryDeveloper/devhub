using DevHub.Api.Extensions;
using DevHub.Application;
using DevHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// The composition root. Each layer registers its own services; Program.cs only wires them
// together and fixes the middleware order, so this file stays readable as the app grows.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Swagger is a development tool and an information leak in production: it publishes every
// route, DTO shape and status code the API has.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "DevHub API v1"));
}

app.UseCors();

// EPIC 2: UseAuthentication() and UseAuthorization() belong here, after UseCors. Put
// authorization first and anonymous CORS preflight requests are rejected before the CORS
// middleware ever sees them, which looks like a browser bug rather than a pipeline bug.

app.MapApiHealthChecks();
app.MapControllers();

app.Run();

/// <summary>Exposed so DEVHUB-011's integration tests can use WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program;
