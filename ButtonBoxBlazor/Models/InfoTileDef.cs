/// <summary>
/// Describes a single read-only telemetry tile in an <c>ItemGrid</c>: its grid position, caption and value formatter.
/// </summary>
public sealed record InfoTileDef(int Row,
                                 int Col,
                                 string Label,
                                 Func<CondorTelemetry, string> Format,
                                 string? Color = "white") : GridItemDef(Row, Col, Color);
