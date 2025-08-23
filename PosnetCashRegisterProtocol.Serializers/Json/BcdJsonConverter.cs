using System.Text.Json;
using System.Text.Json.Serialization;

namespace PosnetCashRegisterProtocol.Serializers.Json;

/// <summary>
/// JSON converter for <see cref="Bcd"/>.
/// </summary>
public class BcdJsonConverter : JsonConverter<Bcd>
{
    public override Bcd Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) => reader.GetInt64();

    public override void Write(
        Utf8JsonWriter writer,
        Bcd value,
        JsonSerializerOptions options) => writer.WriteNumberValue(value);
}
