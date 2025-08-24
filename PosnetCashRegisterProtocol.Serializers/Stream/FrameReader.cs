using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using PosnetCashRegisterProtocol.Enums;
using System.Runtime.CompilerServices;

namespace PosnetCashRegisterProtocol.Serializers.Stream;

/// <summary>
/// <see cref="Frame"/> reading tools.
/// </summary>
public static class FrameReader
{
    /// <summary>
    /// Max <see cref="Frame"/> size.
    /// </summary>
    public static int MaxCapacity { get; set; } = 100 * 1024;

    /// <summary>
    /// Reads a <see cref="Frame"/> from <paramref name="stream"/>, taking into account
    /// occurrences of control characters <see cref="ESpecialChar"/>.
    /// </summary>
    /// <param name="stream">Binary stream.</param>
    /// <param name="onFlush">Invalid data handler.</param>
    /// <returns><see cref="Frame"/>.</returns>
    /// <exception cref="OperationCanceledException">Thrown when an <see cref="ESpecialChar.CAN"/> character is detected.</exception>
    /// <exception cref="InvalidDataException">Thrown when an invalid <see cref="Frame"/> is received.</exception>
    public static Frame ReadFrame(this System.IO.Stream stream, Action<ReadOnlyMemory<byte>, string>? onFlush = null)
    {
        var memory = ReadFrameMemory(stream, onFlush);

        try
        {
            return new Frame(memory);
        }
        catch (Exception ex)
        {
            onFlush?.Invoke(memory, ex.Message);
            throw new InvalidDataException(ex.Message);
        }
    }

    /// <summary>
    /// Reads a frame data from <paramref name="stream"/>, taking into account
    /// occurrences of control characters <see cref="ESpecialChar"/>.
    /// </summary>
    /// <param name="stream">Binary stream.</param>
    /// <param name="onFlush">Invalid data handler.</param>
    /// <returns>Frame memory.</returns>
    /// <exception cref="OperationCanceledException">Thrown when an <see cref="ESpecialChar.CAN"/> character is detected.</exception>
    public static ReadOnlyMemory<byte> ReadFrameMemory(this System.IO.Stream stream, Action<ReadOnlyMemory<byte>, string>? onFlush = null)
    {
        using var buffer = new ArrayPoolBufferWriter<byte>();

        byte value = default;
        bool stxDetected = false;
        bool etxDetected = false;

        while (!etxDetected)
        {
            if (buffer.WrittenCount > MaxCapacity)
            {
                try { onFlush?.Invoke(buffer.WrittenMemory, "Buffer overrun"); }
                catch { }

                buffer.Clear();
                stxDetected = false;
                etxDetected = false;
            }

            value = stream.ReadByte(out var isSpecial);

            if (isSpecial)
            {
                switch (value)
                {
                    case (byte)ESpecialChar.STX:
                        if (buffer.WrittenCount > 0)
                        {
                            try { onFlush?.Invoke(buffer.WrittenMemory, "STX detected"); }
                            catch { }

                            buffer.Clear();
                        }

                        stxDetected = true;
                        break;

                    case (byte)ESpecialChar.ETX:
                        etxDetected = stxDetected;
                        break;

                    case (byte)ESpecialChar.CAN:
                        onFlush?.Invoke(buffer.WrittenMemory, "CAN detected");
                        throw new OperationCanceledException("CAN detected.");
                }
            }

            buffer.Write(value);
        }

        return buffer.WrittenMemory.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ReadByte(this System.IO.Stream stream, out bool isSpecial)
    {
        isSpecial = false;
        var value = stream.Read<byte>();

        if (value == (byte)ESpecialChar.SYN)
        {
            isSpecial = true;
            return stream.Read<byte>();
        }

        return value;
    }
}
