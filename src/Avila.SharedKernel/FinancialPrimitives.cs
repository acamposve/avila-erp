namespace Avila.SharedKernel;

/// <summary>
/// ADR-010 monetary value. Amounts are stored with 4 decimals; rounding is
/// always half-away-from-zero in one place.
/// </summary>
public readonly record struct Money(decimal Amount, string Currency)
{
    public static decimal Round(decimal value, int decimals = 4)
        => decimal.Round(value, decimals, MidpointRounding.AwayFromZero);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount + other.Amount };
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return this with { Amount = Amount - other.Amount };
    }

    public Money Multiply(decimal factor)
        => this with { Amount = Round(Amount * factor, 4) };

    public void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new DomainException(
                $"Cannot combine {Currency} and {other.Currency} without a conversion rate.");
        }
    }

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);
}

public readonly record struct Quantity(decimal Value, string Unit)
{
    public static Quantity Round(decimal value) => new(Money.Round(value, 6), string.Empty);
}

/// <summary>Exchange rate with explicit source/effective date, numeric(19,8).</summary>
public readonly record struct Rate(decimal Value, string FromCurrency, string ToCurrency, DateTimeOffset EffectiveAt)
{
    public decimal Apply(decimal amount) => Money.Round(amount * Value, 4);
}

/// <summary>
/// Document and settlement rates are both retained so realized FX can be
/// computed without retroactive recalculation.
/// </summary>
public readonly record struct CurrencyPair(
    string BaseCurrency,
    string TransactionCurrency,
    decimal DocumentRate,
    decimal SettlementRate)
{
    public decimal RealizedDifference(decimal transactionAmount)
        => Money.Round(
            transactionAmount * (SettlementRate - DocumentRate),
            4);
}
