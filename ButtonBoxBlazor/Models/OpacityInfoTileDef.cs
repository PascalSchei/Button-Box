/// <summary>
/// Describes the tile showing the current LX window opacity.
/// </summary>
public sealed record OpacityInfoTileDef(int Row,
                                        int Col,
                                        string Label = "LX opacity",
                                        string? Color = "violet") : GridItemDef(Row, Col, Color);
