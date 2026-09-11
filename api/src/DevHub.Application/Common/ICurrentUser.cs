namespace DevHub.Application.Common;

/// <summary>
/// The caller behind the current request. Implemented in Infrastructure over
/// IHttpContextAccessor; handlers depend on this interface so they stay testable and so
/// authorization decisions live in the Application layer, not in controllers.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Null when the request is anonymous.</summary>
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    /// <summary>The authenticated user's id, or a throw. Use inside handlers that require auth.</summary>
    Guid UserIdOrThrow => UserId ?? throw new InvalidOperationException("The request is not authenticated.");
}
