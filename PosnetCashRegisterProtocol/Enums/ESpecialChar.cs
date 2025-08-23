namespace PosnetCashRegisterProtocol.Enums;

/// <summary>
/// Frame control characters.
/// </summary>
/// <remarks>
/// <see cref="SKF-I-DEV-49"/> - "SPECYFIKACJA PROTOKOŁU KAS POSNET NEO XL EJ 2.01".
/// </remarks>
public enum ESpecialChar : byte
{
    /// <summary>
    /// Frame start identifier.
    /// </summary>
    /// <remarks>
    /// If received in the middle of a frame, no error is reported, and frame reception restarts from the beginning.
    /// </remarks>
    STX = 0x02,

    /// <summary>
    /// Frame end identifier.
    /// </summary>
    /// <remarks>
    /// If received in the middle of a frame but at an unexpected position (too early), an error is reported.
    /// </remarks>
    ETX = 0x03,

    /// <summary>
    /// The byte preceding the above special characters.
    /// </summary>
    /// <remarks>
    /// Occurrences of this character are not taken into account when calculating the frame length or the checksum.
    /// If a character with the value <see cref="SYN"/> appears in the frame, it must be preceded by a <see cref="SYN"/> byte; 
    /// otherwise, the character following this byte will be treated as a control character,
    /// and the cash register will misinterpret the command sent to it.
    /// </remarks>
    SYN = 0x10,

    /// <summary>
    /// Frame analysis interrupt request character.
    /// </summary>
    /// <remarks>
    /// If no frame has been analyzed, this character is ignored.
    /// </remarks>
    CAN = 0x18,
}
