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
    private const ushort FlagsOffset = 1;
    private const ushort TokenOffset = 3;
    private const ushort FlenOffset = 7;
    private const ushort FldNumOffset = 9;
    private const ushort CommandOffset = 11;
    private const ushort FieldsOffset = 13;

    private static readonly Index CrcOffset = new(3, true);
    private static readonly Encoding TextEncoding;

    private readonly ReadOnlyMemory<byte> _bytes;
    private readonly List<ushort> _indexes;

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
    {
        var memory = CreateFrameMemory(out var indexes, flags, token, command, fields);

        var length = (ushort)0;
        MemoryMarshal.Write(memory.Span[FlenOffset..], in length);

        var crc = CalculateCRC(memory.Span[FlagsOffset..CrcOffset]);
        MemoryMarshal.Write(memory.Span[CrcOffset..], in crc);

        return new Frame(memory, indexes);
    }

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
    {
        var memory = CreateFrameMemory(out _indexes, flags, token, command, fields);

        var crc = CalculateCRC(memory.Span[FlagsOffset..CrcOffset]);
        MemoryMarshal.Write(memory.Span[CrcOffset..], in crc);

        _bytes = memory;
    }

    /// <inheritdoc cref="Frame(ReadOnlyMemory{byte}, ushort)" />
    public Frame(ReadOnlyMemory<byte> bytes) : this(bytes, CalculateCRC(bytes.Span[FlagsOffset..CrcOffset]))
    { }

    /// <summary>
    /// Creates <see cref="Frame"/> from read only memory.
    /// </summary>
    /// <param name="bytes">Binary frame representation without <see cref="Enums.ESpecialChar"/> control characters.</param>
    /// <param name="crc">Frame checksum.</param>
    /// <exception cref="ArgumentException">Thrown when the frame format is invalid.</exception>
    /// <exception cref="InvalidCastException">Thrown when any data field format is invalid.</exception>
    public Frame(ReadOnlyMemory<byte> bytes, ushort crc)
    {
        if (bytes.Span[0] != (byte)ESpecialChar.STX)
        {
            throw new ArgumentException($"Missing STX.");
        }

        if (bytes.Span[^1] != (byte)ESpecialChar.ETX)
        {
            throw new ArgumentException($"Missing ETX.");
        }

        _bytes = bytes;

        if (Crc != crc)
        {
            throw new ArgumentException($"Invalid CRC.", nameof(bytes));
        }

        _indexes = [];
        var offset = FieldsOffset;
        while (offset < bytes.Length - 3)
        {
            _indexes.Add(offset);
            offset += (char)bytes.Span[offset] switch
            {
                'S' => (ushort)(GetTextDataLength(bytes.Span[(offset + 1)..]) + 1),
                'B' => 2, // sizeof(byte) + 1
                'V' => 3, // sizeof(ushort) + 1
                'L' => 5, // sizeof(uint) + 1
                'N' => 7, // size of BCD + 1,
                _ => throw new InvalidCastException($"Not recognized data type: : {bytes.Span[offset]}."),
            };
        }

        if (offset != bytes.Length - 3 || FldNum != _indexes.Count)
        {
            throw new ArgumentException($"Data fields corrupted.");
        }
    }

    private Frame(ReadOnlyMemory<byte> bytes, List<ushort> indexes)
    {
        _bytes = bytes;
        _indexes = indexes;
    }

    private static Memory<byte> CreateFrameMemory(out List<ushort> indexes, ushort flags, uint token, ushort command, params object[] fields)
    {
        ushort flen = FieldsOffset;
        var fldNum = fields.Length;

        indexes = [];
        for (int i = 0; i < fldNum; i++)
        {
            indexes.Add(flen);
            flen += fields[i] switch
            {
                string text => (ushort)(TextEncoding.GetByteCount(text) + 2), // text.Length + \0 + type
                byte => 2,// sizeof(byte) + 1
                ushort => 3,// sizeof(ushort) + 1
                uint => 5,// sizeof(uint) + 1
                Bcd => 7,// size of BCD + 1
                _ => throw new InvalidCastException($"Not allowed data type : {fields[i]}."),
            };
        }

        flen += 3;

        Memory<byte> memory = new byte[flen];
        var span = memory.Span;

        span[0] = (byte)ESpecialChar.STX;
        span[^1] = (byte)ESpecialChar.ETX;

        MemoryMarshal.Write(span[FlagsOffset..], in flags);
        MemoryMarshal.Write(span[TokenOffset..], in token);
        MemoryMarshal.Write(span[FlenOffset..], in flen);
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
                    span[index++] = b;
                    break;

                case ushort v:
                    span[index++] = (byte)'V';
                    MemoryMarshal.Write(span[index..], in v);
                    index += 2;
                    break;

                case uint l:
                    span[index++] = (byte)'L';
                    MemoryMarshal.Write(span[index..], in l);
                    index += 4;
                    break;

                case Bcd n:
                    span[index++] = (byte)'N';
                    n.Bytes().CopyTo(span[index..]);
                    index += 6;
                    break;
            }
        }

        return memory;
    }

    #endregion

    public ushort Flags => MemoryMarshal.AsRef<ushort>(FrameMemory.Span[FlagsOffset..]);

    public uint Token => MemoryMarshal.AsRef<uint>(FrameMemory.Span[TokenOffset..]);

    public ushort FLen => MemoryMarshal.AsRef<ushort>(FrameMemory.Span[FlenOffset..]);

    public ushort FldNum => MemoryMarshal.AsRef<ushort>(FrameMemory.Span[FldNumOffset..]);

    public ushort Command => MemoryMarshal.AsRef<ushort>(FrameMemory.Span[CommandOffset..]);

    public ushort Crc => MemoryMarshal.AsRef<ushort>(FrameMemory.Span[CrcOffset..]);

    public ReadOnlyMemory<byte> FrameMemory => _bytes;

    #region IReadOnlyList implementation

    public int Count => _indexes.Count;

    public object this[int index]
    {
        get
        {
            var offset = _indexes[index];
            return FrameMemory.Span[offset++] switch
            {
                (byte)'S' => TextEncoding.GetString(FrameMemory.Span.Slice(offset, GetTextDataLength(FrameMemory.Span[offset..]) - 1)),
                (byte)'B' => FrameMemory.Span[offset],
                (byte)'V' => MemoryMarshal.AsRef<ushort>(FrameMemory.Span[offset..]),
                (byte)'L' => MemoryMarshal.AsRef<uint>(FrameMemory.Span[offset..]),
                (byte)'N' => new Bcd(FrameMemory.Span.Slice(offset, 6)),
                _ => throw new InvalidCastException($"Not recognized data type : {FrameMemory.Span[_indexes[index]]}."),
            };
        }
    }

    public IEnumerator<object> GetEnumerator() => new FieldsEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class FieldsEnumerator(Frame source) : object(), IEnumerator<object>
    {
        private readonly Frame _enumerated = source;
        private int _index = -1;

        public object Current => _enumerated[_index];

        public void Dispose() { }

        public bool MoveNext() => ++_index < _enumerated.Count;

        public void Reset() => _index = -1;
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
    private static int GetTextDataLength(ReadOnlySpan<byte> bytes)
    {
        var length = bytes.IndexOf((byte)0);
        if (length < 0)
        {
            throw new InvalidCastException($"Missing <Zero> at the end of the text data field.");
        }

        return ++length;
    }
}
