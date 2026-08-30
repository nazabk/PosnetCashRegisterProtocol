using PosnetCashRegisterProtocol.Enums;
using System.Buffers;

namespace PosnetCashRegisterProtocol.Serializers.Stream;

/// <summary>
/// <see cref="Frame"/> writing tools.
/// </summary>
public static class FrameWriter
{
    /// <summary>
    /// Writes a <see cref="Frame"/> to the <paramref name="stream"/>, 
    /// adding <see cref="ESpecialChar"/> control characters.
    /// </summary>
    /// <param name="stream">Binary stream.</param>
    /// <param name="frame"><see cref="Frame"/>.</param>
    public static void WriteFrame(this System.IO.Stream stream, IFrame frame) =>
        WriteFrameMemory(stream, frame.FrameMemory);

    /// <summary>
    /// Writes asynchronously a <see cref="Frame"/> to the <paramref name="stream"/>, 
    /// adding <see cref="ESpecialChar"/> control characters.
    /// </summary>
    /// <param name="stream">Binary stream.</param>
    /// <param name="frame"><see cref="Frame"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    public static ValueTask WriteFrameAsync(
        this System.IO.Stream stream,
        IFrame frame,
        CancellationToken cancellationToken) =>
        WriteFrameMemoryAsync(stream, frame.FrameMemory, cancellationToken);

    /// <summary>
    /// Writes a frame memory to the <paramref name="stream"/>, 
    /// adding <see cref="ESpecialChar"/> control characters.
    /// </summary>
    /// <param name="stream">Binary stream.</param>
    /// <param name="frame"><see cref="Frame"/>.</param>
    public static void WriteFrameMemory(this System.IO.Stream stream, ReadOnlyMemory<byte> memory)
    {
        Span<byte> buffer = stackalloc byte[CalculateBufferLength(memory)];
        Serialize(memory, buffer);
        stream.Write(buffer);
    }

    /// <summary>
    /// Writes asynchronously a frame memory to the <paramref name="stream"/>, 
    /// adding <see cref="ESpecialChar"/> control characters.
    /// </summary>
    /// <param name="stream">Binary stream.</param>
    /// <param name="frame"><see cref="Frame"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    public static async ValueTask WriteFrameMemoryAsync(
        this System.IO.Stream stream,
        ReadOnlyMemory<byte> memory,
        CancellationToken cancellationToken)
    {
        var bufferLength = CalculateBufferLength(memory);
        using var buffer = MemoryPool<byte>.Shared.Rent(bufferLength);
        Serialize(memory, buffer.Memory.Span);
        await stream.WriteAsync(buffer.Memory[..bufferLength], cancellationToken);
    }

    /// <summary>
    /// Adds <see cref="ESpecialChar"/> control characters to <paramref name="frameMemory"/>
    /// and writes it into <paramref name="buffer"/>.
    /// </summary>
    /// <param name="frameMemory">Frame memory.</param>
    /// <param name="buffer">Output buffer.</param>
    public static void Serialize(ReadOnlyMemory<byte> frameMemory, Span<byte> buffer)
    {
        var index = 0;

        buffer[index++] = (byte)ESpecialChar.SYN;
        buffer[index++] = (byte)ESpecialChar.STX;

        for (int i = 1; i < frameMemory.Length - 1; i++)
        {
            if (frameMemory.Span[i] == (byte)ESpecialChar.SYN)
            {
                buffer[index++] = (byte)ESpecialChar.SYN;
            }

            buffer[index++] = frameMemory.Span[i];
        }

        buffer[index++] = (byte)ESpecialChar.SYN;
        buffer[index] = (byte)ESpecialChar.ETX;
    }

    /// <summary>
    /// Caluclates length of <paramref name="memory"/> with added
    /// <see cref="ESpecialChar"/> control characters.
    /// </summary>
    /// <param name="memory">Frame memory <see cref="Frame.FrameMemory"/>.</param>
    /// <returns>Length of <paramref name="memory"/> plus count of added
    /// <see cref="ESpecialChar"/> control characters.</returns>
    public static int CalculateBufferLength(ReadOnlyMemory<byte> memory)
    {
        int length = memory.Length + 2;
        foreach (var mark in memory.Span[1..^1])
        {
            if (mark == (byte)ESpecialChar.SYN)
            {
                length++;
            }
        }

        return length;
    }
}
