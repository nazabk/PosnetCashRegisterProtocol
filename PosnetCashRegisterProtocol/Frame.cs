using PosnetCashRegisterProtocol.Enums;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PosnetCashRegisterProtocol;

/// <summary>
/// Posnet cash register protocol frame.
/// </summary>
public sealed class Frame : IFrame
{
    private const ushort ByteSize = 1;     // sizeof(byte)
    private const ushort ShortSize = 2;    // sizeof(ushort)
    private const ushort LongSize = 4;     // sizeof(uint)
    private const ushort BcdSize = 6;      // size of BCD

    private const int StxOffset = 0;
    private const int FlagsOffset = 1;
    private const int TokenOffset = 3;
    private const int FlenOffset = 7;
    private const int FldNumOffset = 9;
    private const int CommandOffset = 11;
    private const int FieldsOffset = 13;

    private static readonly Index EtxOffset = new(1, true);
    private static readonly Index CrcOffset = new(3, true);
    private static readonly Encoding TextEncoding;

    private readonly ReadOnlyMemory<byte> _frameBytes;

    static Frame()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        TextEncoding = Encoding.GetEncoding(1250);
    }

    #region Constructors & Factory methods

    /// <summary>
    /// Creates <see cref="Frame"/> with <see cref="Frame.FLen"/> set to zero.
    /// </summary>
    /// <param name="flags"><see cref="Frame.Flags"/> value.</param>
    /// <param name="token"><see cref="Frame.Token"/> value.</param>
    /// <param name="command"><see cref="Frame.Command"/> value.</param>
    /// <param name="fields">Data field values.</param>
    /// <returns><see cref="Frame"/>.</returns>
    /// <exception cref="InvalidCastException">Thrown when any data field format is invalid.</exception>
    public static Frame CreateZeroFLen(ushort flags, uint token, ushort command, params object[] fields)
        => new(flags, token, command, fields, flen: 0);

    /// <summary>
    /// Creates <see cref="Frame"/>.
    /// </summary>
    /// <param name="flags"><see cref="Frame.Flags"/> value.</param>
    /// <param name="token"><see cref="Frame.Token"/> value.</param>
    /// <param name="command"><see cref="Frame.Command"/> value.</param>
    /// <param name="fields">Data field values.</param>
    /// <returns><see cref="Frame"/>.</returns>
    /// <exception cref="InvalidCastException">Thrown when any data field format is invalid.</exception>
    public Frame(ushort flags, uint token, ushort command, params object[] fields)
        : this(flags, token, command, fields, flen: null)
    { }

    /// <inheritdoc cref="Frame(ReadOnlyMemory{byte}, ushort)" />
    public Frame(ReadOnlyMemory<byte> memory)
    {
        _frameBytes = memory;

        if (_frameBytes.Span[StxOffset] != (byte)ESpecialChar.STX)
        {
            throw new ArgumentException($"Missing STX.");
        }

        if (_frameBytes.Span[EtxOffset] != (byte)ESpecialChar.ETX)
        {
            throw new ArgumentException($"Missing ETX.");
        }

        if (Crc != CalculateCRC(_frameBytes.Span[FlagsOffset..CrcOffset]))
        {
            throw new ArgumentException($"Invalid CRC.");
        }

        var count = 0;
        var offset = FieldsOffset;
        var length = CrcOffset.GetOffset(_frameBytes.Length);

        while (offset < length)
        {
            count++;
            offset += GetFieldSize(_frameBytes.Span, offset);
        }

        if (offset != length || count != FldNum)
        {
            throw new ArgumentException($"Data fields corrupted.");
        }
    }

    private Frame(ushort flags, uint token, ushort command, object[] fields, ushort? flen)
    {
        ushort length = FieldsOffset;
        var fldNum = fields.Length;

        for (int i = 0; i < fldNum; i++)
        {
            length += GetFieldSize(fields[i]);
        }

        length += (ushort)CrcOffset.Value;

        Memory<byte> memory = new byte[length];
        var span = memory.Span;

        span[StxOffset] = (byte)ESpecialChar.STX;
        span[EtxOffset] = (byte)ESpecialChar.ETX;

        if (flen.HasValue)
        {
            length = flen.Value;
        }

        MemoryMarshal.Write(span[FlagsOffset..], in flags);
        MemoryMarshal.Write(span[TokenOffset..], in token);
        MemoryMarshal.Write(span[FlenOffset..], in length);
        MemoryMarshal.Write(span[FldNumOffset..], in fldNum);
        MemoryMarshal.Write(span[CommandOffset..], in command);

        var index = FieldsOffset;
        for (int i = 0; i < fldNum; i++)
        {
            switch (fields[i])
            {
                case string text:
                    span[index++] = (byte)'S';
                    var bytes = TextEncoding.GetBytes(text);
                    bytes.CopyTo(span[index..]);
                    index += (ushort)bytes.Length;
                    span[index++] = 0;
                    break;

                case byte b:
                    span[index++] = (byte)'B';
                    span[index] = b;
                    index += ByteSize;
                    break;

                case ushort v:
                    span[index++] = (byte)'V';
                    MemoryMarshal.Write(span[index..], in v);
                    index += ShortSize;
                    break;

                case uint l:
                    span[index++] = (byte)'L';
                    MemoryMarshal.Write(span[index..], in l);
                    index += LongSize;
                    break;

                case Bcd n:
                    span[index++] = (byte)'N';
                    n.Bytes().CopyTo(span[index..]);
                    index += BcdSize;
                    break;
            }
        }

        var crc = CalculateCRC(span[FlagsOffset..CrcOffset]);
        MemoryMarshal.Write(span[CrcOffset..], in crc);

        _frameBytes = memory;
    }

    #endregion

    public ushort Flags => MemoryMarshal.AsRef<ushort>(FrameMemory.Span[FlagsOffset..]);

    public uint Token => MemoryMarshal.AsRef<uint>(FrameMemory.Span[TokenOffset..]);

    public ushort FLen => MemoryMarshal.AsRef<ushort>(FrameMemory.Span[FlenOffset..]);

    public ushort FldNum => MemoryMarshal.AsRef<ushort>(FrameMemory.Span[FldNumOffset..]);

    public ushort Command => MemoryMarshal.AsRef<ushort>(FrameMemory.Span[CommandOffset..]);

    public ushort Crc => MemoryMarshal.AsRef<ushort>(FrameMemory.Span[CrcOffset..]);

    public ReadOnlyMemory<byte> FrameMemory => _frameBytes;

    #region IReadOnlyCollection implementation

    public int Count => FldNum;

    public IEnumerator<object> GetEnumerator() => new FieldsEnumerator(FrameMemory);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class FieldsEnumerator(ReadOnlyMemory<byte> bytes) : object(), IEnumerator<object>
    {
        private readonly int _offsetLimit = CrcOffset.GetOffset(bytes.Length);

        private int _offset;
        private int _shift = FieldsOffset;

        public object Current => GetField(bytes.Span, _offset);

        public void Dispose() { }

        public bool MoveNext()
        {
            _offset += _shift;

            if (_offset < _offsetLimit)
            {
                _shift = GetFieldSize(bytes.Span, _offset);

                return true;
            }

            return false;
        }

        public void Reset()
        {
            _offset = 0;
            _shift = FieldsOffset;
        }
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort CalculateCRC(ReadOnlySpan<byte> data)
    {
        ushort crc = 0;
        foreach (byte b in data)
        {
            crc = (ushort)((crc >> 8) | (crc << 8));
            crc ^= b;
            crc ^= (ushort)((crc & 0xff) >> 4);
            crc ^= (ushort)(crc << 8 << 4);
            crc ^= (ushort)((crc & 0xff) << 4 << 1);
        }

        return crc;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetTextLength(ReadOnlySpan<byte> bytes)
    {
        var length = bytes.IndexOf((byte)0);
        if (length < 0)
        {
            throw new InvalidCastException($"Missing <Zero> at the end of the text data field.");
        }

        return ++length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort GetFieldSize(ReadOnlySpan<byte> frameBytes, int offset) => (char)frameBytes[offset] switch
    {
        'S' => (ushort)(GetTextLength(frameBytes[(offset + 1)..]) + 1),
        'B' => ByteSize + 1,
        'V' => ShortSize + 1,
        'L' => LongSize + 1,
        'N' => BcdSize + 1,
        _ => throw new InvalidCastException($"Not recognized data type: {frameBytes[offset]} at {offset}."),
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort GetFieldSize(object field) => field switch
    {
        string text => (ushort)(TextEncoding.GetByteCount(text) + 2), // text.Length + \0 + type
        byte => ByteSize + 1,
        ushort => ShortSize + 1,
        uint => LongSize + 1,
        Bcd => BcdSize + 1,
        _ => throw new InvalidCastException($"Not allowed data type : {field}."),
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object GetField(ReadOnlySpan<byte> frameBytes, int offset) => (char)frameBytes[offset++] switch
    {
        'S' => TextEncoding.GetString(frameBytes.Slice(offset, GetTextLength(frameBytes[offset..]) - 1)),
        'B' => frameBytes[offset],
        'V' => MemoryMarshal.AsRef<ushort>(frameBytes[offset..]),
        'L' => MemoryMarshal.AsRef<uint>(frameBytes[offset..]),
        'N' => new Bcd(frameBytes.Slice(offset, 6)),
        _ => throw new InvalidCastException($"Not recognized data type : {frameBytes[offset]} at {offset}."),
    };
}
