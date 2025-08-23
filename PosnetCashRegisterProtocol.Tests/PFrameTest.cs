using PosnetCashRegisterProtocol.Serializers.Json;
using PosnetCashRegisterProtocol.Serializers.Stream;
using PosnetCashRegisterProtocol.Tests.DataSources;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PosnetCashRegisterProtocol.Tests;

public partial class FrameTest
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new FrameConverter(), new JsonStringEnumConverter() },
        WriteIndented = true,
    };

    [Theory]
    [ClassData(typeof(FrameDataSource))]
    public void FrameFromBinaryTest(byte[] data, ushort flags, uint token, ushort flen, ushort fldnum, ushort cmd, ushort crc, object[] fields)
    {
        //arrange
        var inputStream = new MemoryStream(data);
        var outputStream = new MemoryStream();

        //act
        var pf = inputStream.ReadFrame();
        outputStream.WriteFrame(pf);

        //assert
        Assert.Equal(flags, pf.Flags);
        Assert.Equal(token, pf.Token);
        Assert.Equal(flen, pf.FLen);
        Assert.Equal(fldnum, pf.FldNum);
        Assert.Equal(pf.Count, pf.FldNum);
        Assert.Equal(cmd, pf.Command);
        Assert.Equal(crc, pf.Crc);
        Assert.Equal(fields, [.. pf]);
        Assert.Equal(data, outputStream.ToArray());
    }

    [Theory]
    [ClassData(typeof(FrameDataSource))]
    public void FrameFromScratchTest(byte[] data, ushort flags, uint token, ushort flen, ushort fldnum, ushort cmd, ushort crc, object[] fields)
    {
        //arrange
        var inputStream = new MemoryStream(data);
        var outputStream = new MemoryStream();

        //act
        var pf = inputStream.ReadFrame();
        outputStream.WriteFrame(pf);

        //assert
        Assert.Equal(flags, pf.Flags);
        Assert.Equal(token, pf.Token);
        Assert.Equal(flen, pf.FLen);
        Assert.Equal(fldnum, pf.FldNum);
        Assert.Equal(pf.Count, pf.FldNum);
        Assert.Equal(cmd, pf.Command);
        Assert.Equal(crc, pf.Crc);
        Assert.Equal(fields, [.. pf]);
        Assert.Equal(data, outputStream.ToArray());
    }

    [Theory]
    [ClassData(typeof(DetectFrameTestData))]
    public void DetectFrameTest(byte[] data, byte[] frame)
    {
        //arrange
        var inputStream = new MemoryStream(data);
        var outputStream = new MemoryStream();

        //act
        var Frame = inputStream.ReadFrame();
        outputStream.WriteFrame(Frame);

        //assert
        Assert.Equal(frame, outputStream.ToArray());
    }

    [Theory]
    [ClassData(typeof(JsonDeserializeDataSource))]
    public void FrameFromJsonTest(string data, ushort flags, uint token, ushort flen, ushort fldnum, ushort cmd, ushort crc, object[] fields)
    {
        //arrange
        //act
        var pf = JsonSerializer.Deserialize<Frame>(data, Options)!;

        //assert
        Assert.Equal(flags, pf.Flags);
        Assert.Equal(token, pf.Token);
        Assert.Equal(flen, pf.FLen);
        Assert.Equal(fldnum, pf.FldNum);
        Assert.Equal(cmd, pf.Command);
        Assert.Equal(pf.Count, pf.FldNum);
        Assert.Equal(crc, pf.Crc);
        Assert.Equal(fields, [.. pf]);
    }

    [Theory]
    [ClassData(typeof(JsonSerializeDataSource))]
    public void FrameToJsonTest(string data, ushort flags, uint token, ushort flen, ushort fldnum, ushort cmd, ushort crc, object[] fields)
    {
        //arrange
        //act
        var pf = flen > 0
            ? new Frame(flags, token, cmd, fields)
            : Frame.CreateZeroFLen(flags, token, cmd, fields);
        var result = JsonSerializer.Serialize(pf, Options);

        //assert
        Assert.Equal(flags, pf.Flags);
        Assert.Equal(token, pf.Token);
        Assert.Equal(flen, pf.FLen);
        Assert.Equal(fldnum, pf.FldNum);
        Assert.Equal(cmd, pf.Command);
        Assert.Equal(pf.Count, pf.FldNum);
        Assert.Equal(crc, pf.Crc);
        Assert.Equal(data, result);
    }
}
