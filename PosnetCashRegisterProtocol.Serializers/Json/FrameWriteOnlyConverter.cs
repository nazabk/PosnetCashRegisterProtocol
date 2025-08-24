using PosnetCashRegisterProtocol.Enums;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PosnetCashRegisterProtocol.Serializers.Json;

/// <summary>
/// JSON converter for <see cref="IFrame"/>.
/// </summary>
public class FrameWriteOnlyConverter : JsonConverter<IFrame>
{
    private const string DataFieldsName = "Fields";

    public override IFrame Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Deserialization is not supported.");
    }

    public override void Write(Utf8JsonWriter writer, IFrame frame, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(GetValidName(nameof(Frame.Flags), options));
        JsonSerializer.Serialize(writer, (EFlag)frame.Flags, options);

        writer.WritePropertyName(GetValidName(nameof(Frame.Token), options));
        JsonSerializer.Serialize(writer, frame.Token, options);

        writer.WritePropertyName(GetValidName(nameof(Frame.FLen), options));
        JsonSerializer.Serialize(writer, frame.FLen, options);

        writer.WritePropertyName(GetValidName(nameof(Frame.FldNum), options));
        JsonSerializer.Serialize(writer, frame.FldNum, options);

        writer.WritePropertyName(GetValidName(nameof(Frame.Command), options));
        JsonSerializer.Serialize(writer, (ECommand)frame.Command, options);

        if (frame.Count > 0)
        {
            writer.WritePropertyName(GetValidName(DataFieldsName, options));
            JsonSerializer.Serialize(writer, frame.Select(x => x.SerializeDataField()), options);
        }

        writer.WritePropertyName(GetValidName(nameof(Frame.Crc), options));
        JsonSerializer.Serialize(writer, frame.Crc, options);

        writer.WriteEndObject();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetValidName(string name, JsonSerializerOptions options) =>
        options.PropertyNamingPolicy?.ConvertName(name) ?? name;
}
