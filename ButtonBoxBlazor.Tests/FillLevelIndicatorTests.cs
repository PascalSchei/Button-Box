public class FillLevelIndicatorTests
{
    private static readonly DateTime T0 = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Empty_HasNoFillAndNoBlink()
    {
        var indicator = new FillLevelIndicator();

        indicator.Update(0, T0);

        Assert.Equal(0, indicator.FillFraction);
        Assert.False(indicator.IsBlinking);
    }

    [Fact]
    public void FirstNonZeroValue_FillsCompletely_WithoutBlinking()
    {
        var indicator = new FillLevelIndicator();

        indicator.Update(150, T0);

        Assert.Equal(1, indicator.FillFraction);
        Assert.False(indicator.IsBlinking);
    }

    [Fact]
    public void Draining_ShrinksFillRelativeToInitialMaximum()
    {
        var indicator = new FillLevelIndicator();

        indicator.Update(150, T0);
        indicator.Update(75, T0);

        Assert.Equal(0.5, indicator.FillFraction, 3);
    }

    [Fact]
    public void Draining_StartsBlinkingImmediately()
    {
        var indicator = new FillLevelIndicator();

        indicator.Update(150, T0);
        indicator.Update(149, T0);

        Assert.True(indicator.IsBlinking);
    }

    [Fact]
    public void Blinking_ContinuesWithinHoldAndStopsAfter()
    {
        var indicator = new FillLevelIndicator(TimeSpan.FromSeconds(2));

        indicator.Update(150, T0);
        indicator.Update(149, T0);

        indicator.Tick(T0.AddSeconds(1));
        Assert.True(indicator.IsBlinking);

        indicator.Tick(T0.AddSeconds(2));
        Assert.False(indicator.IsBlinking);
    }

    [Fact]
    public void OngoingChanges_ExtendTheBlinkWindow()
    {
        var indicator = new FillLevelIndicator(TimeSpan.FromSeconds(2));

        indicator.Update(150, T0);
        indicator.Update(149, T0);
        indicator.Update(148, T0.AddSeconds(1.5));

        indicator.Tick(T0.AddSeconds(2));
        Assert.True(indicator.IsBlinking);

        indicator.Tick(T0.AddSeconds(3.5));
        Assert.False(indicator.IsBlinking);
    }

    [Fact]
    public void ReachingZero_StopsBlinkingAndClearsFill()
    {
        var indicator = new FillLevelIndicator();

        indicator.Update(150, T0);
        indicator.Update(10, T0);
        Assert.True(indicator.IsBlinking);

        indicator.Update(0, T0);

        Assert.Equal(0, indicator.FillFraction);
        Assert.False(indicator.IsBlinking);
    }

    [Fact]
    public void RefillAfterEmpty_UsesNewMaximum()
    {
        var indicator = new FillLevelIndicator();

        indicator.Update(150, T0);
        indicator.Update(0, T0);
        indicator.Update(100, T0);
        indicator.Update(50, T0);

        Assert.Equal(0.5, indicator.FillFraction, 3);
    }

    [Fact]
    public void IncreasingValue_DoesNotBlink()
    {
        var indicator = new FillLevelIndicator();

        indicator.Update(100, T0);
        indicator.Update(150, T0);

        Assert.Equal(1, indicator.FillFraction);
        Assert.False(indicator.IsBlinking);
    }
}
