namespace PosnetCashRegisterProtocol;

/// <summary>
/// Posnet cash register protocol frame.
/// </summary>
/// <remarks>
/// Based on <see cref="SKF-I-DEV-49"/> - "SPECYFIKACJA PROTOKOŁU KAS POSNET NEO XL EJ 2.01".
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "IFrame is more than just fields collection.")]
public interface IFrame : IReadOnlyCollection<object>
{
    /// <summary>
    /// Frame field <see cref="FLAGS"/>.
    /// </summary>
    ushort Flags { get; }

    /// <summary>
    /// Frame field <see cref="TOKEN"/>.
    /// </summary>
    uint Token { get; }

    /// <summary>
    /// Frame field <see cref="F_LEN"/>.
    /// </summary>
    ushort FLen { get; }

    /// <summary>
    /// Frame field <see cref="FLD_NUM"/>.
    /// </summary>
    ushort FldNum { get; }

    /// <summary>
    /// Frame field <see cref="CMD_ID"/>.
    /// </summary>
    ushort Command { get; }

    /// <summary>
    /// Frame field <see cref="CRC"/>.
    /// </summary>
    ushort Crc { get; }

    /// <summary>
    /// Binary representation of the frame without <see cref="Enums.ESpecialChar"/> control characters.
    /// </summary>
    ReadOnlyMemory<byte> FrameMemory { get; }
}
