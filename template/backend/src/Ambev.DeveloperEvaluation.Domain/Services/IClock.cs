namespace Ambev.DeveloperEvaluation.Domain.Services;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
