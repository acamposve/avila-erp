namespace Avila.SharedKernel;

public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public readonly record struct TenantId(Guid Value)
{
    public static TenantId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}
