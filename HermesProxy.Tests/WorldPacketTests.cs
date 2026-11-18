using System.IO.Compression;
using HermesProxy.World;
using Xunit;

namespace HermesProxy.Tests;

public class WorldPacketTests
{
    [Fact]
    public void InflatePreservesPayloadOpcodeAndReceiveTime()
    {
        var payload = Enumerable.Range(0, 4096).Select(i => (byte)(i % 123)).ToArray();
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(payload);

        using var packet = new WorldPacket(1, compressed.ToArray());
        packet.SetReceiveTime(1234);
        using var inflated = packet.Inflate(payload.Length);

        Assert.Equal(payload, inflated.ReadToEnd());
        Assert.Equal(packet.GetOpcode(), inflated.GetOpcode());
        Assert.Equal(packet.GetReceivedTime(), inflated.GetReceivedTime());
    }
}
