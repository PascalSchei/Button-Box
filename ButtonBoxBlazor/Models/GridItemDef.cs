/// <summary>
/// Common base of everything placed in an <c>ItemGrid</c>: grid position and tint color.
/// </summary>
/// <param name="Color">Any CSS color; tints the item like an LED.</param>
public record GridItemDef(int Row, int Col, string? Color)
{
    /// <summary>Inline style placing the item in the grid and exposing <c>--btn-color</c>.</summary>
    public string Style
    {
        get
        {
            var style = $"grid-row:{Row + 1}; grid-column:{Col + 1};";
            return Color is { } color ? style + $"--btn-color:{color};" : style;
        }
    }
}
