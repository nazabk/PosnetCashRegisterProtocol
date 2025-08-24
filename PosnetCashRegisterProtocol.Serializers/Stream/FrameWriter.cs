using PosnetCashRegisterProtocol.Enums;

namespace PosnetCashRegisterProtocol.Serializers.Stream;

/// <summary>
/// <see cref="Frame"/> writing tools.
/// </summary>
public static class FrameWriter
{
    private static readonly byte[] Specials = [(byte)ESpecialChar.SYN, (byte)ESpecialChar.STX, (byte)ESpecialChar.ETX];

    /// <summary>
    /// Writes a <see cref="Frame"/> to the <paramref name="stream"/>, adding <see cref="ESpecialChar"/> control characters.
    /// </summary>
    /// <param name="stream">Binary stream.</param>
    /// <param name="frame"><see cref="Bcd"/>.</param>
    public static void WriteFrame(this System.IO.Stream stream, Frame frame) => WriteFrameMemory(stream, frame.FrameMemory);

    /// <summary>
    /// Writes a frame memory to the <paramref name="stream"/>, adding <see cref="ESpecialChar"/> control characters.
    /// </summary>
    /// <param name="stream">Binary stream.</param>
    /// <param name="frame"><see cref="Bcd"/>.</param>
    public static void WriteFrameMemory(this System.IO.Stream stream, ReadOnlyMemory<byte> memory)
    {
        stream.Write(Specials, 0, 1);
        stream.Write(Specials, 1, 1);

        var data = memory.Span[1..^1];
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] == (byte)ESpecialChar.SYN)
            {
                stream.Write(Specials, 0, 1);
            }

            stream.WriteByte(data[i]);
        }

        stream.Write(Specials, 0, 1);
        stream.Write(Specials, 2, 1);
    }
}
