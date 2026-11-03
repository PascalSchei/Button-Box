public class HudMessageHistoryTests
{
    private static string[] Run(params string[] packets)
    {
        var history = new HudMessageHistory(3);
        foreach (var packet in packets)
        {
            history.Update(packet);
        }

        return [.. history.Entries];
    }

    [Fact]
    public void RepeatedPacket_IsAddedOnce() =>
        Assert.Equal(["a", "b"], Run("a;b", "a;b", "a;b"));

    [Fact]
    public void ScrollingWindow_AppendsOnlyNewMessages() =>
        Assert.Equal(["a", "b", "c"], Run("a", "a;b", "b;c"));

    [Fact]
    public void SameTextAfterEmptyPacket_ReplacesOlderEntry() =>
        Assert.Equal(["a"], Run("a", "", "a"));

    [Fact]
    public void GameTime_IsPrefixedAndRepeatedMessageKeepsNewestTime()
    {
        var history = new HudMessageHistory();
        history.Update("Hans started Race", 12.5833);
        history.Update("Hans started Race;Hans landed", 12.6);
        history.Update("", 13.0);
        history.Update("Hans started Race", 13.25);

        Assert.Equal(["12:36: Hans landed", "13:15: Hans started Race"], history.Entries);
    }

    [Fact]
    public void Capacity_DropsOldestEntries() =>
        Assert.Equal(["b", "c", "d"], Run("a", "b", "c", "d"));
}
