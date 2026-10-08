/// <summary>
/// Describes a button that changes the LX window opacity by <paramref name="Delta"/> percent.
/// </summary>
public sealed record OpacityButtonDef(int Row,
                                       int Col,
                                       string Label,
                                       string Icon,
                                       int Delta,
                                       string? Color = "violet") : GridItemDef(Row, Col, Color);
