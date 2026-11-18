using HermesProxy.Enums;
using HermesProxy.World;
using HermesProxy.World.Objects;
using Xunit;

namespace HermesProxy.Tests;

public class ItemTemplateTests
{
    [Theory]
    [InlineData(ClientVersionBuild.V1_12_1_5875, false, false)]
    [InlineData(ClientVersionBuild.V2_4_3_8606, true, false)]
    [InlineData(ClientVersionBuild.V3_3_5a_12340, true, true)]
    public void ReadsLegacyItemTemplate(ClientVersionBuild build, bool hasTbcFields, bool hasWrathFields)
    {
        using var fixture = new WorldPacket(1);
        fixture.WriteInt32(2); // Class.
        fixture.WriteUInt32(3);
        if (hasTbcFields)
            fixture.WriteInt32(11);

        fixture.WriteCString("Test sword");
        for (int i = 0; i < 3; i++)
            fixture.WriteCString(string.Empty);
        fixture.WriteUInt32(3141); // Display ID.
        fixture.WriteInt32(3); // Quality.
        fixture.WriteUInt32(128); // Flags.
        if (hasWrathFields)
            fixture.WriteUInt32(2048);

        WriteZeroWords(5); // Prices, inventory type, and class/race masks.
        fixture.WriteUInt32(45); // Item level.
        fixture.WriteUInt32(32); // Required level.
        WriteZeroWords(10); // Skill, spell, ranks, reputation, counts, and slots.

        int statCount = hasWrathFields ? 11 : 10;
        if (hasWrathFields)
            fixture.WriteUInt32((uint)statCount);
        for (int i = 0; i < statCount; i++)
        {
            fixture.WriteInt32(100 + i);
            fixture.WriteInt32(200 + i);
        }
        if (hasWrathFields)
        {
            fixture.WriteInt32(17);
            fixture.WriteUInt32(18);
        }

        for (int i = 0; i < (hasWrathFields ? 2 : 5); i++)
        {
            fixture.WriteFloat(i + 1);
            fixture.WriteFloat(i + 2);
            fixture.WriteInt32(i);
        }
        WriteZeroWords(10); // Armor, resistances, delay, ammo, and ranged mod.

        for (int i = 0; i < 5; i++)
        {
            fixture.WriteInt32(i == 2 ? 42 : 0);
            WriteZeroWords(5); // Spell trigger, charges, cooldowns, and category.
        }

        fixture.WriteInt32(2); // Bonding.
        fixture.WriteCString("Fixture description");
        WriteZeroWords(5); // Page, language, page material, quest, and lock.
        fixture.WriteInt32(-1); // Material is normalized to zero.
        WriteZeroWords(2); // Sheath type and random property.
        if (hasTbcFields)
            fixture.WriteUInt32(23); // Random suffix.
        WriteZeroWords(5); // Block, set, durability, area, and map.
        fixture.WriteUInt32(128); // Bag family.

        if (hasTbcFields)
        {
            fixture.WriteInt32(5); // Totem category.
            WriteZeroWords(2);
            fixture.WriteInt32(4); // Second socket color.
            fixture.WriteUInt32(900);
            WriteZeroWords(6); // Third socket, bonus, gem, disenchant skill, armor mod.
            fixture.WriteUInt32(60); // Duration.
        }
        if (hasWrathFields)
        {
            fixture.WriteInt32(24); // Item limit category.
            fixture.WriteInt32(25); // Holiday ID.
        }

        using var packet = new WorldPacket(1, fixture.GetData());
        var item = new ItemTemplate();
        item.ReadFromLegacyPacket(123, packet, build);

        Assert.Empty(packet.ReadToEnd());
        Assert.Equal(123u, item.Entry);
        Assert.Equal("Test sword", item.Name[0]);
        Assert.Equal(3141u, item.DisplayID);
        Assert.Equal(45u, item.ItemLevel);
        Assert.Equal(32u, item.RequiredLevel);
        Assert.Equal((uint)statCount, item.StatsCount);
        Assert.Equal(199 + statCount, item.StatValues[statCount - 1]);
        Assert.Equal(hasWrathFields ? 0f : 5f, item.DamageMins[4]);
        Assert.Equal(42, item.TriggeredSpellIds[2]);
        Assert.Equal("Fixture description", item.Description);
        Assert.Equal(0, item.Material);
        Assert.Equal(128u, item.BagFamily);
        Assert.Equal(hasTbcFields ? 11 : 0, item.SoundOverrideSubclass);
        Assert.Equal(hasTbcFields ? 23u : 0u, item.RandomSuffix);
        Assert.Equal(hasTbcFields ? 900u : 0u, item.SocketContent[1]);
        Assert.Equal(hasTbcFields ? 60u : 0u, item.Duration);
        Assert.Equal(hasWrathFields ? 2048u : 0u, item.FlagsExtra);
        Assert.Equal(hasWrathFields ? 18u : 0u, item.ScalingStatValue);
        Assert.Equal(hasWrathFields ? 25 : 0, item.HolidayID);

        void WriteZeroWords(int count)
        {
            for (int i = 0; i < count; i++)
                fixture.WriteUInt32(0);
        }
    }
}
