/// <summary>
/// How an <c>ItemGrid</c> button reacts to being pressed.
/// </summary>
public enum ButtonMode
{
    /// <summary>Key is held down for as long as the button is pressed (e.g. Push To Talk).</summary>
    Hold,

    /// <summary>Sends repeated full press/release pulses while held (e.g. volume up/down).</summary>
    Repeat
}

/// <summary>
/// Describes a single button in an <c>ItemGrid</c>: its grid position, the key it sends, its function label and the key cap text.
/// </summary>
/// <param name="Tag">Key name (or '+'-joined combo) understood by <see cref="Key.TryParse"/>.</param>
/// <param name="KeyCaption">Text shown on the key cap when the device shows the key option.</param>
/// <param name="Function">Condor function number used in controls.ini; when bound there to a keyboard key, that key replaces <paramref name="Tag"/>.</param>
public sealed record ButtonDef(int Row,
                               int Col,
                               string Tag,
                               string Label,
                               string KeyCaption,
                               string? Icon = null,
                               string? Color = null,
                               ButtonMode Mode = ButtonMode.Hold,
                               int? Function = null) : GridItemDef(Row, Col, Color);
