using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using Framework;
using Framework.Constants;
using HermesProxy.Enums;
using HermesProxy.World.Client;
using HermesProxy.World.Enums;
using Xunit;

namespace HermesProxy.Tests;

public class WorldClientTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    public WorldClientTests()
    {
        Settings.ClientBuild = ClientVersionBuild.V1_14_2_42597;
        Settings.ServerBuild = ClientVersionBuild.V1_12_1_5875;
    }

    [Theory]
    [InlineData(AuthResult.AUTH_OK, true, 0u)]
    [InlineData(AuthResult.AUTH_WAIT_QUEUE, true, 17u)]
    [InlineData(AuthResult.AUTH_FAILED, false, 0u)]
    [InlineData(null, false, 0u)]
    public async Task WorldConnectCompletesWhenServerRepliesOrCloses(
        AuthResult? response, bool expected, uint expectedQueuePosition)
    {
        using var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();
        var realm = new Realm
        {
            ExternalAddress = IPAddress.Loopback.ToString(),
            Port = (ushort)((IPEndPoint)listener.LocalEndpoint).Port
        };
        var client = new WorldClient();
        var session = new GlobalSessionData { Username = "test", WorldClient = client };
        var connect = Task.Factory.StartNew(() => client.ConnectToWorldServer(realm, session),
            CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        using var peer = await listener.AcceptTcpClientAsync().WaitAsync(TestTimeout);
        try
        {
            Assert.False(connect.IsCompleted);
            if (response.HasValue)
            {
                // This fixture tests how the receive loop signals a result, not legacy credentials.
                bool queued = response == AuthResult.AUTH_WAIT_QUEUE;
                var packet = new byte[4 + 1 + 4 + 1 + 4 + (queued ? 4 : 0)];
                BinaryPrimitives.WriteUInt16BigEndian(packet, (ushort)(packet.Length - 2));
                BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2),
                    checked((ushort)LegacyVersion.GetCurrentOpcode(Opcode.SMSG_AUTH_RESPONSE)));
                packet[4] = (byte)response.Value;
                if (queued)
                    BinaryPrimitives.WriteUInt32LittleEndian(packet.AsSpan(14), expectedQueuePosition);
                await peer.GetStream().WriteAsync(packet);
            }
            else
            {
                peer.Close();
            }

            Assert.Equal(expected, await connect.WaitAsync(TestTimeout));
            Assert.Equal(expected, client.IsAuthenticated());
            Assert.Equal(expectedQueuePosition, client.GetQueuePosition());
        }
        finally
        {
            if (client.IsAuthenticated())
                client.Disconnect();
        }
    }
}
