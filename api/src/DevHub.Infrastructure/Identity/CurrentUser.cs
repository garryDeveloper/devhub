using DevHub.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DevHub.Infrastructure.Identity;

/// <summary>
/// <see cref="ICurrentUser"/> over the request's <see cref="HttpContext.User"/>, which JwtBearer
/// has already populated by the time any handler runs (DEVHUB-018).
/// </summary>
/// <remarks>
/// Reads the <c>sub</c> claim as-is: inbound claim mapping is off (see AddIdentity), so it is the
/// same name TokenService wrote. Read on every access rather than cached — the object is scoped,
/// and computing it lazily costs nothing.
/// <para>
/// Outside a request (a background job) there is no HttpContext, and the caller is anonymous.
/// </para>
/// </remarks>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            // We sign every token and always write a Guid, so a non-Guid "sub" is a bug on our
            // side, not a caller. Treated as anonymous so UserId and IsAuthenticated never
            // disagree; UserIdOrThrow then fails loudly instead of handing out Guid.Empty.
            return Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId)
                ? userId
                : null;
        }
    }

    public bool IsAuthenticated => UserId is not null;
}
