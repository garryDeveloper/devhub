namespace DevHub.Domain.Common;

/// <summary>
/// Marker for something that happened in the domain, stated in the past tense
/// (IssueStatusChanged, DeploymentSucceeded). Deliberately empty: adding a timestamp here
/// would push the domain towards DateTime.UtcNow, and time is an injected dependency
/// (ITimeProvider) in this codebase.
/// </summary>
public interface IDomainEvent
{
}
