/// <summary>
/// Describes a single read-only telemetry tile in an <c>ItemGrid</c>: its grid position, caption and value formatter.
/// </summary>
/// <param name="Level">When set, the tile shows a fill-level gauge in its background driven by this telemetry value (e.g. water ballast).</param>
public sealed record InfoTileDef(int Row,
                                 int Col,
                                 string Label,
                                 Func<CondorTelemetry, string> Format,
                                 string? Color = null,
                                 Func<CondorTelemetry, double>? Level = null) : GridItemDef(Row, Col, Color);
