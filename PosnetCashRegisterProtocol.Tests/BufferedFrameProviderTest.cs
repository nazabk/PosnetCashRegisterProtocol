using PosnetCashRegisterProtocol.Serializers.Stream;
using PosnetCashRegisterProtocol.Tests.DataSources;

namespace PosnetCashRegisterProtocol.Tests;

public partial class BufferedFrameProviderTest
{
    [Theory]
    [ClassData(typeof(FrameDataSource))]
    public void FrameFromBinaryTest(byte[] data, ushort flags, uint token, ushort flen, ushort fldnum, ushort cmd, ushort crc, object[] fields)
    {
        //arrange
        var frameProvider = new BufferedFrameProvider();

        //act
        var raisedEvent = Assert.Raises<Frame>(
            attach: handler => frameProvider.FrameRead += handler,
            detach: handler => frameProvider.FrameRead -= handler,
            testCode: () => frameProvider.AddData(data));

        var frame = raisedEvent.Arguments;

        //assert
        Assert.Equal(flags, frame.Flags);
        Assert.Equal(token, frame.Token);
        Assert.Equal(flen, frame.FLen);
        Assert.Equal(fldnum, frame.FldNum);
        Assert.Equal(frame.Count, frame.FldNum);
        Assert.Equal(cmd, frame.Command);
        Assert.Equal(crc, frame.Crc);
        Assert.Equal(fields, [.. frame]);
    }
}
