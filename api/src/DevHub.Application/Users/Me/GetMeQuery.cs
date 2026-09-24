namespace DevHub.Application.Users.Me;

/// <summary><c>GET /api/me</c>. No parameters: "me" is always <c>ICurrentUser</c>, never an id from the request.</summary>
public sealed record GetMeQuery;
