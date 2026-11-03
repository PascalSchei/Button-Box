/// <summary>
/// Multi-line tile showing Condor's HUD messages. Size it with <see cref="GridItemDef.RowSpan"/> and <see cref="GridItemDef.ColSpan"/>.
/// </summary>
public sealed record HudMessagesDef(int Row,
                                    int Col,
                                    string? Color = null) : GridItemDef(Row, Col, Color);
