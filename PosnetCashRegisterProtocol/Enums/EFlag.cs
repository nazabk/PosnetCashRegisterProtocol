namespace PosnetCashRegisterProtocol.Enums;

/// <summary>
/// Protocol frame flags.
/// </summary>
/// <remarks>
/// <see cref="SKF-I-DEV-49"/> - "SPECYFIKACJA PROTOKOŁU KAS POSNET NEO XL EJ 2.01".
/// </remarks>
[Flags]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Compliance with the documentation: SKF-I-DEV-49")]
public enum EFlag : ushort
{
    /// <summary>
    /// No flags.
    /// </summary>
    NONE = 0x00,

    /// <summary>
    /// Frame structure verification without providing data; only the protocol syntax is checked.
    /// </summary>
    VERIFY = 0x08,

    /// <summary>
    /// Repeating the frame with the specified token.
    /// </summary>
    /// <remarks>
    /// It is possible to query the cash register for a lost or incorrect response frame.
    /// To do this, a frame with the <see cref="REPEAT"/> flag set in the <see cref="Frame.Flags"/>
    /// field must be sent. The <see cref="Frame.Token"/> and <see cref="Frame.Command"/> fields
    /// must be identical to those in the previously sent command; other fields are ignored. 
    /// The buffer length in the cash register is approximately 2 kB. If a command with the given token
    /// and command identifier is not found in the buffer, the <see cref="EError.COMM_ERR_FRAME_LOST"/>
    /// error is returned. When the <see cref="REPEAT"/> flag is set, syntax errors relate
    /// to the query sequence for repeating the response, not the original sequence.
    /// </remarks>
    REPEAT = 0x10,
}
