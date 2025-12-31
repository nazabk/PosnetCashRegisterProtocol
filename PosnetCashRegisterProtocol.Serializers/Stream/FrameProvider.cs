using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using PosnetCashRegisterProtocol.Enums;
using System.Diagnostics.CodeAnalysis;

namespace PosnetCashRegisterProtocol.Serializers.Stream;

/// <summary>
/// <see cref="Frame"/> reading tools.
/// </summary>
public sealed class FrameProvider(System.IO.Stream stream, int bufferCapacity = 100 * 1024) : IDisposable
{
    private readonly ArrayPoolBufferWriter<byte> _frameBuffer = new(bufferCapacity);
    private readonly byte[] _inputBuffer = new byte[bufferCapacity];
    private readonly System.IO.Stream _inputStream = stream;

    private int _index;
    private int _count;

    /// <summary>
    /// Max stream data length without <see cref="Frame"/>.
    /// </summary>
    public int BufferCapacity => _inputBuffer.Length;

    /// <summary>
    /// Occurs when invalid data received, providing the flushed data as a read-only memory buffer.
    /// </summary>
    public event EventHandler<(ReadOnlyMemory<byte> Memory, string Reason)>? Flush;

    /// <summary>
    /// Gets a <see cref="Frame"/> from internal stream, taking into account
    /// occurrences of control characters <see cref="ESpecialChar"/>.
    /// </summary>
    /// <returns><see cref="Frame"/>.</returns>
    /// <exception cref="OperationCanceledException">Thrown when an <see cref="ESpecialChar.CAN"/>
    /// character is detected.</exception>
    /// <exception cref="InvalidDataException">Thrown when an invalid <see cref="Frame"/>
    /// is received.</exception>
    public Frame GetFrame()
    {
        var memory = GetFrameMemory();

        try
        {
            return new Frame(memory);
        }
        catch (Exception ex)
        {
            Flush?.Invoke(this, (memory, ex.Message));
            throw new InvalidDataException(ex.Message);
        }
    }

    /// <summary>
    /// Gets a <see cref="Frame"/> from internal stream, taking into account
    /// occurrences of control characters <see cref="ESpecialChar"/>.
    /// </summary>
    /// <returns><see cref="Frame"/>.</returns>
    /// <exception cref="OperationCanceledException">Thrown when an <see cref="ESpecialChar.CAN"/>
    /// character is detected.</exception>
    /// <exception cref="InvalidDataException">Thrown when an invalid <see cref="Frame"/>
    /// is received.</exception>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask<Frame> GetFrameAsync(CancellationToken cancellationToken)
    {
        var memory = await GetFrameMemoryAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return new Frame(memory);
        }
        catch (Exception ex)
        {
            Flush?.Invoke(this, (memory, ex.Message));
            throw new InvalidDataException(ex.Message);
        }
    }

    /// <summary>
    /// Gets a frame data from internal stream, taking into account
    /// occurrences of control characters <see cref="ESpecialChar"/>.
    /// </summary>
    /// <returns>Frame memory.</returns>
    /// <exception cref="OperationCanceledException">Thrown when an <see cref="ESpecialChar.CAN"/>
    /// character is detected.</exception>
    public ReadOnlyMemory<byte> GetFrameMemory()
    {
        while (true)
        {
            if (TryReadFrameMemory(out var frameMemory))
            {
                return frameMemory;
            }
            
            if (_count == 1)
            {
                _inputBuffer[0] = (byte)ESpecialChar.SYN;
            }

            _count += _inputStream.Read(_inputBuffer, _count, _inputBuffer.Length - _count);
            _index = 0;
        }
    }

    /// <summary>
    /// Gets a frame data from internal stream, taking into account
    /// occurrences of control characters <see cref="ESpecialChar"/>.
    /// </summary>
    /// <returns>Frame memory.</returns>
    /// <exception cref="OperationCanceledException">Thrown when an <see cref="ESpecialChar.CAN"/>
    /// character is detected.</exception>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask<ReadOnlyMemory<byte>> GetFrameMemoryAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            if (TryReadFrameMemory(out var frameMemory))
            {
                return frameMemory;
            }

            if (_count == 1)
            {
                _inputBuffer[0] = (byte)ESpecialChar.SYN;
            }

            _count += await _inputStream.ReadAsync(_inputBuffer.AsMemory()[_count..], cancellationToken).ConfigureAwait(false);
            _index = 0;
        }
    }

    public void Dispose()
    {
        _frameBuffer.Dispose();
    }

    private bool TryReadFrameMemory([MaybeNullWhen(false)] out byte[] frameMemory)
    {
        while (TryReadByte(out var value, out var isSpecial))
        {
            if (isSpecial)
            {
                switch ((ESpecialChar)value)
                {
                    case ESpecialChar.STX:
                        ClearBuffer("STX detected");
                        _frameBuffer.Write(value);
                        continue;

                    case ESpecialChar.ETX:
                        _frameBuffer.Write(value);
                        frameMemory = _frameBuffer.WrittenMemory.ToArray();
                        _frameBuffer.Clear();
                        return true;

                    case ESpecialChar.CAN:
                        ClearBuffer("CAN detected");
                        throw new OperationCanceledException("CAN detected.");
                }
            }

            if (_frameBuffer.WrittenCount >= BufferCapacity)
            {
                ClearBuffer("Buffer overrun");
            }

            _frameBuffer.Write(value);
        }

        frameMemory = null;
        return false;
    }

    private bool TryReadByte(out byte value, out bool isSpecial)
    {
        value = 0;
        isSpecial = false;

        if (_count < 1)
        {
            return false;
        }

        value = _inputBuffer[_index];

        if (value == (byte)ESpecialChar.SYN)
        {
            if (_count < 2)
            {
                return false;
            }

            _index++;
            _count--;

            isSpecial = true;
            value = _inputBuffer[_index];
        }

        _index++;
        _count--;

        return true;
    }

    private void ClearBuffer(string message)
    {
        try
        {
            if (_frameBuffer.WrittenCount > 0)
            {
                Flush?.Invoke(this, (_frameBuffer.WrittenMemory, message));
            }
        }
        catch { }

        _frameBuffer.Clear();
    }
}
