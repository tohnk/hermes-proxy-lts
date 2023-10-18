using System.Security.Cryptography;
using HermesProxy.World;
using HermesProxy.World.Server;
using Xunit;

namespace HermesProxy.Tests;

public class RealmAuthSeedTests
{
    private static readonly byte[] StaticSeed = Enumerable.Repeat((byte)0x11, 16).ToArray();
    private static readonly byte[] PlatformSeed = Enumerable.Repeat((byte)0x22, 16).ToArray();
    private static readonly byte[] OtherSeed = Enumerable.Repeat((byte)0x33, 16).ToArray();
    private static readonly byte[] MacArmSeed = Enumerable.Repeat((byte)0x44, 16).ToArray();

    [Fact]
    public void MissingCsvLeavesStaticSeedAsTheOnlyChoice()
    {
        var missingFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".csv");
        Assert.Empty(GameData.LoadBuildAuthSeeds(missingFile));
    }

    [Fact]
    public void ReadsSeedsByBuildAndPlatform()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, $"build,platform,seed,comment\n" +
                $"42597,Wn64,{Convert.ToHexString(PlatformSeed)},\n" +
                $"42597,Mc64,{Convert.ToHexString(OtherSeed)},\n" +
                $"42597,MacA,{Convert.ToHexString(MacArmSeed)},\n" +
                $"40618,Wn64,{Convert.ToHexString(StaticSeed)},\n");
            var builds = GameData.LoadBuildAuthSeeds(path);
            Assert.Equal(PlatformSeed, builds[42597]["Wn64"]);
            Assert.Equal(OtherSeed, builds[42597]["Mc64"]);
            Assert.Equal(MacArmSeed, builds[42597]["MacA"]);
            Assert.Equal(StaticSeed, builds[40618]["Wn64"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("Wn64", "platform", true)]
    [InlineData("Wn64", "static", true)]
    [InlineData("Mc64", "static", true)]
    [InlineData("Mc64", "platform", false)]
    [InlineData("Wn64", "other", false)]
    [InlineData("MacA", "mac", true)]
    [InlineData("MacA", "static", true)]
    [InlineData("Wn64", "mac", false)]
    public void ChecksPlatformSeedAndStaticFallback(string platform, string seedKind, bool expected)
    {
        byte[] seed = seedKind switch
        {
            "static" => StaticSeed,
            "platform" => PlatformSeed,
            "mac" => MacArmSeed,
            _ => OtherSeed
        };

        var build = new RealmBuildInfo { FallbackStaticSeed = StaticSeed };
        build.BuildSeeds.Add("Wn64", PlatformSeed);
        build.BuildSeeds.Add("MacA", MacArmSeed);

        byte[] sessionKey = Enumerable.Range(1, 40).Select(i => (byte)i).ToArray();
        byte[] clientChallenge = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();
        byte[] serverChallenge = Enumerable.Range(20, 16).Select(i => (byte)i).ToArray();
        byte[] authCheck = Convert.FromHexString("C5C69895763F1DCDB6A13728B312FF8A");
        byte[] key = SHA256.HashData([.. sessionKey, .. seed]);
        byte[] challenge = [.. clientChallenge, .. serverChallenge, .. authCheck];
        byte[] digest = HMACSHA256.HashData(key, challenge)[..24];

        Assert.Equal(expected, WorldSocket.VerifyRealmAuthDigest(build, platform, sessionKey,
            clientChallenge, serverChallenge, digest));
    }
}
