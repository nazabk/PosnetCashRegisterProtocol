using System.Globalization;

namespace PosnetCashRegisterProtocol.Serializers.Json;

public static class DataFieldExtensions
{
    public static object DeserializeDataField(this string value)
    {
        return value[0] switch
        {
            'S' => value[1..],
            'B' => byte.Parse(value.AsSpan()[1..], CultureInfo.InvariantCulture),
            'V' => ushort.Parse(value.AsSpan()[1..], CultureInfo.InvariantCulture),
            'L' => uint.Parse(value.AsSpan()[1..], CultureInfo.InvariantCulture),
            'N' => (Bcd)long.Parse(value.AsSpan()[1..], CultureInfo.InvariantCulture),
            _ => throw new InvalidCastException($"Not supported type of data: {value[0]}"),
        };
    }

    public static string SerializeDataField(this object value)
    {
        return value switch
        {
            string s => $"S{s}",
            byte b => FormattableString.Invariant($"B{b}"),
            ushort sh => FormattableString.Invariant($"V{sh}"),
            uint i => FormattableString.Invariant($"L{i}"),
            Bcd n => FormattableString.Invariant($"N{n.Value}"),
            _ => throw new InvalidOperationException($"Not supported type of data: {value.GetType().Name}"),
        };
    }
}
