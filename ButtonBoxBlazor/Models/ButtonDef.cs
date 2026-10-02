/// <summary>
/// How a <c>ButtonGrid</c> button reacts to being pressed.
/// </summary>
public enum ButtonMode
{
    /// <summary>Key is held down for as long as the button is pressed (e.g. Push To Talk).</summary>
    Hold,

    /// <summary>Sends repeated full press/release pulses while held (e.g. volume up/down).</summary>
    Repeat
}

/// <summary>
/// Describes a single button in a <c>ButtonGrid</c>: its grid position, the key it sends, its function label and the key cap text.
/// </summary>
/// <param name="Color">Any CSS color; tints the button face like an LED, border/text/icon stay white.</param>
public sealed record ButtonDef(int Row, int Col, string Tag, string Label, string Key, string? Icon = null, string? Color = null, ButtonMode Mode = ButtonMode.Hold);
