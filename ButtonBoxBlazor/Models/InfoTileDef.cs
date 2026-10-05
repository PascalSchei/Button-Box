/// <summary>
/// Describes a single read-only telemetry tile in a <c>ButtonGrid</c>: its grid position, caption and value formatter.
/// </summary>
public sealed record InfoTileDef(int Row, int Col, string Label, Func<CondorTelemetry, string> Format);
