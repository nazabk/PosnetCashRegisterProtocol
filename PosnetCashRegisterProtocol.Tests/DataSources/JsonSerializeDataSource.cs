using PosnetCashRegisterProtocol.Enums;
using System.Collections;

namespace PosnetCashRegisterProtocol.Tests.DataSources;

public class JsonSerializeDataSource : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        var testData = new[]
        {
@"{
  ""Flags"": ""NONE"",
  ""Token"": 3,
  ""FLen"": 18,
  ""FldNum"": 1,
  ""Command"": ""SALERECGET"",
  ""Fields"": [
    ""B0""
  ],
  ""Crc"": 34507
}",
@"{
  ""Flags"": ""NONE"",
  ""Token"": 4,
  ""FLen"": 80,
  ""FldNum"": 16,
  ""Command"": ""SALERECGET"",
  ""Fields"": [
    ""B0"",
    ""V0"",
    ""V18"",
    ""B31"",
    ""B12"",
    ""V2020"",
    ""B16"",
    ""B8"",
    ""S001"",
    ""SKASJER"",
    ""V2"",
    ""V2"",
    ""N300"",
    ""N300"",
    ""L0"",
    ""N0""
  ],
  ""Crc"": 60962
}",
@"{
  ""Flags"": ""NONE"",
  ""Token"": 1,
  ""FLen"": 16,
  ""FldNum"": 0,
  ""Command"": ""CASHREGSTATUSGET"",
  ""Crc"": 35284
}",
@"{
  ""Flags"": ""NONE"",
  ""Token"": 1,
  ""FLen"": 112,
  ""FldNum"": 22,
  ""Command"": ""CASHREGSTATUSGET"",
  ""Fields"": [
    ""B24"",
    ""S001"",
    ""V0"",
    ""B1"",
    ""B0"",
    ""B1"",
    ""B0"",
    ""B29"",
    ""B7"",
    ""V2019"",
    ""B18"",
    ""B14"",
    ""B37"",
    ""B31"",
    ""B1"",
    ""V2021"",
    ""B0"",
    ""B6"",
    ""B10"",
    ""S811-174-67-06"",
    ""SBDY 12057651"",
    ""SPOSNET NEO XL EJ 2.01""
  ],
  ""Crc"": 19176
}",
@"{
  ""Flags"": ""NONE"",
  ""Token"": 2,
  ""FLen"": 16,
  ""FldNum"": 0,
  ""Command"": ""BILLBUFCFGGETEX"",
  ""Crc"": 36803
}",
@"{
  ""Flags"": ""NONE"",
  ""Token"": 2,
  ""FLen"": 27,
  ""FldNum"": 5,
  ""Command"": ""BILLBUFCFGGETEX"",
  ""Fields"": [
    ""B1"",
    ""B1"",
    ""V8500"",
    ""B0"",
    ""B0""
  ],
  ""Crc"": 56174
}",
@"{
  ""Flags"": ""NONE"",
  ""Token"": 1608,
  ""FLen"": 28,
  ""FldNum"": 4,
  ""Command"": ""ERROR"",
  ""Fields"": [
    ""V451"",
    ""V0"",
    ""V801"",
    ""V0""
  ],
  ""Crc"": 24841
}"
        };

        yield return new object[]
        {
            testData[0],
            (ushort)EFlag.NONE,
            (uint)3,
            (ushort)18,
            (ushort)1,
            (ushort)ECommand.SALERECGET,
            (ushort)34507,
            new object[]
            {
                (byte)0
            }
        };
        yield return new object[]
        {
            testData[1],
            (ushort)EFlag.NONE,
            (uint)4,
            (ushort)80,
            (ushort)16,
            (ushort)ECommand.SALERECGET,
            (ushort)60962,
            new object[]
            {
                (byte)0,
                (ushort)0,
                (ushort)18,
                (byte)31,
                (byte)12,
                (ushort)2020,
                (byte)16,
                (byte)8,
                "001",
                "KASJER",
                (ushort)2,
                (ushort)2,
                (Bcd)300,
                (Bcd)300,
                (uint)0,
                (Bcd)0
            }
        };
        yield return new object[]
        {
            testData[2],
            (ushort)EFlag.NONE,
            (uint)1,
            (ushort)16,
            (ushort)0,
            (ushort)ECommand.CASHREGSTATUSGET,
            (ushort)35284,
            Array.Empty<object>()
        };
        yield return new object[]
        {
            testData[3],
            (ushort)EFlag.NONE,
            (uint)1,
            (ushort)112,
            (ushort)22,
            (ushort)ECommand.CASHREGSTATUSGET,
            (ushort)19176,
            new object[]
            {
                (byte)24,
                "001",
                (ushort)0,
                (byte)1,
                (byte)0,
                (byte)1,
                (byte)0,
                (byte)29,
                (byte)7,
                (ushort)2019,
                (byte)18,
                (byte)14,
                (byte)37,
                (byte)31,
                (byte)1,
                (ushort)2021,
                (byte)0,
                (byte)6,
                (byte)10,
                "811-174-67-06",
                "BDY 12057651",
                "POSNET NEO XL EJ 2.01",
            }
        };
        yield return new object[]
        {
            testData[4],
            (ushort)EFlag.NONE,
            (uint)2,
            (ushort)16,
            (ushort)0,
            (ushort)ECommand.BILLBUFCFGGETEX,
            (ushort)36803,
            Array.Empty<object>()
        };
        yield return new object[]
        {
            testData[5],
            (ushort)EFlag.NONE,
            (uint)2,
            (ushort)27,
            (ushort)5,
            (ushort)ECommand.BILLBUFCFGGETEX,
            (ushort)56174,
            new object[]
            {
                (byte)1,
                (byte)1,
                (ushort)8500,
                (byte)0,
                (byte)0,
            }
        };
        yield return new object[]
        {
            testData[6],
            (ushort)EFlag.NONE,
            (uint)1608,
            (ushort)28,
            (ushort)4,
            (ushort)ECommand.ERROR,
            (ushort)24841,
            new object[]
            {
                (ushort)451,
                (ushort)0,
                (ushort)801,
                (ushort)0
            }
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
