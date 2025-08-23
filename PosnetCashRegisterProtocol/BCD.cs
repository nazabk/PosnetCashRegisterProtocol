namespace PosnetCashRegisterProtocol;

/// <summary>
/// Values representing amounts or quantities, in signed BCD format.
/// </summary>
/// <seealso href="https://en.wikipedia.org/wiki/Binary-coded_decimal"/>
public record struct Bcd
{
    private const long _baseValue = 1_000_000_000_000L;

    public const long Max = _baseValue / 2 - 1;
    public const long Min = -(_baseValue / 2);

    /// <inheritdoc cref="Bcd(ReadOnlySpan{byte}"/>
    public Bcd(byte[] data) : this(data.AsSpan())
    { }

    /// <summary>
    /// Creates <see cref="BCD"/>.
    /// </summary>
    /// <param name="data">Binary representation of <see cref="BCD"/> value.</param>
    /// <exception cref="ArgumentException">Thrown when invalid <paramref name="data"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any of <paramref name="data"/> is out of the range <c>[0.. 0x99]</c>.</exception>
    public Bcd(ReadOnlySpan<byte> data)
    {
        if (data.Length != 6)
        {
            throw new ArgumentException("Invalid length - must be 6", nameof(data));
        }

        long result = 0;
        foreach (var b in data)
        {
            if (b > 0x99)
            {
                throw new ArgumentOutOfRangeException(nameof(data), "Invalid bytes - must be less or equal 0x99");
            }

            result = result * 100 + (b >> 4) * 10 + (b & 0x0F);
        }

        Value = (result - Min) % _baseValue + Min;
    }

    /// <summary>
    /// Creates <see cref="BCD"/>.
    /// </summary>
    /// <param name="d">The value must be in the range <c>[<see cref="Min"/>..<see cref="Max"/>]</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="d"/> is out of the range <c>[<see cref="Min"/>..<see cref="Max"/>]</c>.</exception>
    public Bcd(long d)
    {
        if (d > Max || d < Min)
        {
            throw new ArgumentOutOfRangeException($"Accepted range:<{Min}, {Max}>.");
        }

        Value = d;
    }

    /// <summary>
    /// Max <see cref="Bcd"/> value: 499 999 999 999.
    /// </summary>
    public static readonly Bcd MaxValue = new(Max);

    /// <summary>
    /// Min <see cref="Bcd"/> value: -500 000 000 000.
    /// </summary>
    public static readonly Bcd MinValue = new(Min);

    /// <summary>
    /// Decimal <see cref="Bcd"/> value.
    /// </summary>
    public long Value { get; }

    /// <summary>
    /// Converts decimal values into <see cref="Bcd"/>.
    /// </summary>
    /// <param name="d">The value must be in the range <c>[<see cref="Min"/>..<see cref="Max"/>]</c>.</param>
    public static implicit operator Bcd(long d) => new(d);

    /// <summary>
    /// Converts <see cref="Bcd"/> into decimal.
    /// </summary>
    /// <param name="d"><see cref="Bcd"/> value.</param>
    public static implicit operator long(Bcd d) => d.Value;

    /// <summary>
    /// Gets <see cref="Bcd"/> binary representation.
    /// </summary>
    /// <returns>Array of bytes.</returns>
    public readonly byte[] Bytes()
    {
        var d = (_baseValue + Value) % _baseValue;
        var result = new byte[6];
        for (uint i = 0; i < 6; i++)
        {
            var tmp = d % 100;
            result[5 - i] = Convert.ToByte(((tmp / 10) << 4) + tmp % 10);
            d /= 100;
        }

        return result;
    }

    /// <inheritdoc/>
    public override readonly int GetHashCode() => Value.GetHashCode();
}
