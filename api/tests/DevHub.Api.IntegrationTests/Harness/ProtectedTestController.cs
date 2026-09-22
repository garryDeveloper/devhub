using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevHub.Api.IntegrationTests.Harness;

/// <summary>
/// Test-only endpoint at <c>GET /api/test/whoami</c> that requires a valid access token and echoes
/// its subject. Lives in the test assembly, so it can never ship.
/// </summary>
[ApiController]
[Route("test/whoami")]
[Authorize]
public sealed class ProtectedTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { Sub = User.FindFirst("sub")?.Value });
}
