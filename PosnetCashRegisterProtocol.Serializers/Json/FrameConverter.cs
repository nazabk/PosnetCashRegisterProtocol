using PosnetCashRegisterProtocol.Enums;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PosnetCashRegisterProtocol.Serializers.Json;

/// <summary>
/// JSON converter for <see cref="Frame"/>.
/// </summary>
public class FrameConverter : JsonConverter<Frame>
{
    private const string DataFieldsName = "Fields";

    public override Frame Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var framePropertyNames = new Dictionary<string, string>()
        {
            { GetValidName(nameof(Frame.Flags), options), nameof(Frame.Flags)},
            { GetValidName(nameof(Frame.Token), options), nameof(Frame.Token)},
            { GetValidName(nameof(Frame.FLen), options), nameof(Frame.FLen)},
            { GetValidName(nameof(Frame.FldNum), options), nameof(Frame.FldNum)},
            { GetValidName(nameof(Frame.Command), options), nameof(Frame.Command)},
            { GetValidName(DataFieldsName, options), DataFieldsName},
            { GetValidName(nameof(Frame.Crc), options), nameof(Frame.Crc)},
        };

        EFlag flags = default;
        uint token = default;
        ushort flen = default;
        ushort fldNum = default;
        ECommand cmd = default;
        ushort crc = default;
        object[] fields = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (framePropertyNames.Count > 1 && framePropertyNames.First().Value != DataFieldsName)
                {
                    throw new InvalidDataException($"Missing fields: {string.Join(", ", framePropertyNames.Values)}");
                }

                var frame = flen > 0
                    ? new Frame((ushort)flags, token, (ushort)cmd, fields)
                    : Frame.CreateZeroFLen((ushort)flags, token, (ushort)cmd, fields);

                if (frame.Crc != crc)
                {
                    throw new InvalidDataException("Invalid CRC.");
                }

                if (frame.FldNum > 0 && frame.FldNum != fldNum)
                {
                    throw new InvalidDataException("Invalid FLD_NUM.");
                }

                if (frame.FLen != flen)
                {
                    throw new InvalidDataException("Invalid FLEN.");
                }

                return frame;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Missing property name.");
            }

            var propertyName = reader.GetString() ?? throw new JsonException("Missing property name.");
            if (framePropertyNames.Remove(propertyName, out var name))
            {
                switch (name)
                {
                    case nameof(Frame.Flags):
                        flags = JsonSerializer.Deserialize<EFlag>(ref reader, options);
                        break;

                    case nameof(Frame.Token):
                        token = JsonSerializer.Deserialize<uint>(ref reader, options);
                        break;

                    case nameof(Frame.FLen):
                        flen = JsonSerializer.Deserialize<ushort>(ref reader, options);
                        break;

                    case nameof(Frame.FldNum):
                        fldNum = JsonSerializer.Deserialize<ushort>(ref reader, options);
                        break;

                    case nameof(Frame.Command):
                        cmd = JsonSerializer.Deserialize<ECommand>(ref reader, options);
                        break;

                    case DataFieldsName:
                        fields = JsonSerializer.Deserialize<List<string>>(ref reader, options)
                            ?.Select(x => x.DeserializeDataField()).ToArray() ?? [];
                        break;

                    case nameof(Frame.Crc):
                        crc = JsonSerializer.Deserialize<ushort>(ref reader, options);
                        break;
                }
            }
            else
            {
                reader.Skip();
            }
        }

        throw new JsonException();
    }

    public override void Write(
        Utf8JsonWriter writer,
        Frame frame,
        JsonSerializerOptions options)
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
