namespace Ambev.DeveloperEvaluation.Domain.Exceptions;

public sealed class IdempotencyConflictException : Exception
{
    public IdempotencyConflictException(string message) : base(message)
    {
    }
}