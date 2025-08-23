namespace PosnetCashRegisterProtocol.Tests;

public class UnitTestBCD
{
    [Theory]
    [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x75 }, 75)]
    [InlineData(new byte[] { 0x99, 0x99, 0x99, 0x99, 0x99, 0x99 }, -1)]
    [InlineData(new byte[] { 0x99, 0x99, 0x99, 0x99, 0x94, 0x45 }, -555)]
    [InlineData(new byte[] { 0x49, 0x99, 0x99, 0x99, 0x99, 0x99 }, 499999999999L)]
    [InlineData(new byte[] { 0x50, 0x00, 0x00, 0x00, 0x00, 0x00 }, -500000000000L)]
    public void LongToBcd(byte[] data, long bcd)
    {
        //Arrange
        //Act
        Bcd value = bcd;
        var bytes = value.Bytes();

        //Assert
        Assert.Equal(data, bytes);
    }

    [Theory]
    [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x75 }, 75)]
    [InlineData(new byte[] { 0x99, 0x99, 0x99, 0x99, 0x99, 0x99 }, -1)]
    [InlineData(new byte[] { 0x99, 0x99, 0x99, 0x99, 0x94, 0x45 }, -555)]
    [InlineData(new byte[] { 0x49, 0x99, 0x99, 0x99, 0x99, 0x99 }, 499999999999L)]
    [InlineData(new byte[] { 0x50, 0x00, 0x00, 0x00, 0x00, 0x00 }, -500000000000L)]
    public void BcdToLong(byte[] data, long bcd)
    {
        //Arrange
        //Act
        long value = new Bcd(data);

        //Assert
        Assert.Equal(bcd, value);
    }
}
