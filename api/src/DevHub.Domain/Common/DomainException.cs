namespace DevHub.Domain.Common;

/// <summary>
/// Thrown when an operation would leave an aggregate in an invalid state — an illegal status
/// transition, a rule the caller is not allowed to break. The API's exception middleware
/// translates it into a 409/422 ProblemDetails; it is never a 500.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
