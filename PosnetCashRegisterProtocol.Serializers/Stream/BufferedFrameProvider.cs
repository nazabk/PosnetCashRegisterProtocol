using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using PosnetCashRegisterProtocol.Enums;

namespace PosnetCashRegisterProtocol.Serializers.Stream;

/// <summary>
/// <see cref="Frame"/> reading tools.
/// </summary>
public sealed class BufferedFrameProvider(int bufferCapacity = 1024) : IDisposable
{
    private readonly ArrayPoolBufferWriter<byte> _buffer = new(bufferCapacity);
    private readonly Lock _lock = new();

    private bool _disposed;

    private bool _synDetected;

    /// <summary>
    /// <see cref="Frame"/> buffer capacity.
    /// </summary>
    public int BufferCapacity => bufferCapacity;

    /// <summary>
    /// Occurs when invalid data received, providing the flushed data as a read-only memory buffer.
    /// </summary>
    public event EventHandler<(ReadOnlyMemory<byte> Memory, string Reason)>? Flush;

    /// <summary>
    /// Occurs when <see cref="Frame"/> is detected in input data.
    /// </summary>
    public event EventHandler<Frame>? FrameRead;

    /// <summary>
    /// Adds new data and detects <see cref="Frame"/>.
    /// </summary>
    /// <param name="data">Input data.</param>
    public void AddData(ReadOnlySpan<byte> data)
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ReadFrames(data, _buffer, ref _synDetected);
        }
    }

    /// <summary>
    /// Reset provider and clears its buffer.
    /// </summary>
    public void Reset()
    {
        _synDetected = false;
        DumpBuffer(_buffer, "Reset");
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if(_disposed) return;
            _buffer.Dispose();
            _disposed = true;
        }
    }

    private void ReadFrames(ReadOnlySpan<byte> data, ArrayPoolBufferWriter<byte> buffer, ref bool synDetected)
    {
        var count = 0;

        if(synDetected)
        {
            if (!TryReadSpecialByte(data, out var value))
            {
                return;
            }

            ProcessSpecialByte(value, buffer);
            count++;

            if (buffer.WrittenCount >= BufferCapacity)
            {
                DumpBuffer(buffer, "Buffer overflow");
            }
        }

        bool isSpecial;
        while (TryReadByte(data[count..], out var value, out isSpecial))
        {
            if (isSpecial)
            {
                ProcessSpecialByte(value, buffer);
                count += 2;
            }
            else
            {
                buffer.Write(value);
                count++;
            }

            if (buffer.WrittenCount >= BufferCapacity)
            {
                DumpBuffer(buffer, "Buffer overflow");
            }
        }

        synDetected = isSpecial;
    }

    private void ProcessSpecialByte(byte value, ArrayPoolBufferWriter<byte> buffer)
    {
        switch ((ESpecialChar)value)
        {
            case ESpecialChar.STX:
                DumpBuffer(buffer, "STX detected");
                buffer.Write(value);
                break;

            case ESpecialChar.ETX:
                buffer.Write(value);
                ReadFrame(buffer);
                break;

            case ESpecialChar.CAN:
                DumpBuffer(buffer, "CAN detected");
                break;

            default:
                buffer.Write(value);
                break;
        }
    }

    private void DumpBuffer(ArrayPoolBufferWriter<byte> buffer, string message)
    {
        try
        {
            if (buffer.WrittenCount > 0)
            {
                Flush?.Invoke(this, (buffer.WrittenMemory, message));
            }
        }
        finally 
        {
            buffer.Clear(); 
        }       
    }

    private void ReadFrame(ArrayPoolBufferWriter<byte> buffer)
    {
        Frame frame;
        try
        {
            frame = new Frame(buffer.WrittenMemory.ToArray());           
        }
        catch (Exception ex)
        {
            DumpBuffer(buffer, $"Invalid data: {ex.Message}");
            return;
        }

        buffer.Clear();
        FrameRead?.Invoke(this, frame);
    }

    private static bool TryReadByte(ReadOnlySpan<byte> data, out byte value, out bool isSpecial)
    {
        if (data.Length == 0)
        {
            value = 0;
            isSpecial = false;
            return false;
        }
                
        if (data[0] == (byte)ESpecialChar.SYN)
        {
            isSpecial = true;                        
            return TryReadSpecialByte(data[1..], out value);
        }

        value = data[0];
        isSpecial = false;
        return true;
    }

    private static bool TryReadSpecialByte(ReadOnlySpan<byte> data, out byte value)
    {
        if (data.Length == 0)
        {
            value = default;
            return false;
        }

        value = data[0];
        return true;
    }
}
