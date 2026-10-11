using System.Globalization;

/// <summary>
/// All available blocks in their default order. Item positions inside a block are relative to its origin;
/// the <c>LayoutPacker</c> decides where a block ends up.
/// </summary>
public static class BlockCatalog
{
    public const int MaxRecommendedSize = 3;

    private const int Step = 10;

    // Blocks below must stay above All (static initializers run in textual order).

    private static readonly GridBlock LxPad = new("lx-pad", "LX Pad",
        new ButtonDef(0, 1, "NumPad8", "LX-Stick up", "Num 8", "icons/arrow-up.svg"),
        new ButtonDef(0, 0, "NumPad7", "Previous mode", "Num 7", "icons/arrow-double-left.svg", Color: "white"),
        new ButtonDef(1, 0, "NumPad4", "LX-Stick left<br>Zoom out", "Num 4", "icons/arrow-left.svg"),
        new ButtonDef(1, 1, "NumPad5", "OK", "Num 5", "icons/ok-circle.svg", Color: "limegreen"),
        new ButtonDef(2, 1, "NumPad2", "LX-Stick down", "Num 2", "icons/arrow-down.svg"),
        new ButtonDef(1, 2, "NumPad6", "LX-Stick right<br>Zoom in", "Num 6", "icons/arrow-right.svg"),
        new ButtonDef(2, 2, "NumPad3", "Abort", "Num 3", "icons/abort-cross.svg", Color: "red"),
        new ButtonDef(0, 2, "NumPad9", "Next mode", "Num 9", "icons/arrow-double-right.svg", Color: "white"));

    private static readonly GridBlock LxTurnpoint = new("lx-turnpoint", "LX Turnpoint",
        new ButtonDef(0, 1, "Prior", "Info Next<br>Turnpoint", "Page Up", "icons/arrow-triangle-up.svg", Color: "turquoise"),
        new ButtonDef(1, 1, "Next", "Info Previous<br>Turnpoint", "Page Down", "icons/arrow-triangle-down.svg", Color: "turquoise"),
        new ButtonDef(0, 0, "Slash", "Switch AAT<br>Turnpoint", "/", "icons/arrow-double-right.svg", Color: "turquoise"),
        new ButtonDef(1, 0, "RightShift", "AAT-Turnpoint Shift On/Off", "Right Shift", "icons/arrow-four-way.svg", Color: "turquoise"));

    private static readonly GridBlock LxFlightInfo = new("lx-flight-info", "Flight info",
        new InfoTileDef(0, 0, "Time", t => TimeSpan.FromHours(t.Time % 24).ToString(@"hh\:mm\:ss")),
        new InfoTileDef(1, 0, "Water", t => t.Water.ToString("F0", CultureInfo.InvariantCulture) + " kg"),
        new InfoTileDef(2, 0, "Above ground", t => t.Height.ToString("F0", CultureInfo.InvariantCulture) + " m"));

    private static readonly GridBlock LxMacCready = new("lx-mac-cready", "MacCready",
        new ButtonDef(0, 0, "Home", "Increase MacCready", "Home", "icons/arrow-triangle-up.svg", Mode: ButtonMode.Repeat, Color: "gold"),
        new InfoTileDef(1, 0, "MacCready m/s", t => t.MacCready.ToString("F2", CultureInfo.InvariantCulture), Color: "gold"),
        new ButtonDef(2, 0, "End", "Decrease MacCready", "End", "icons/arrow-triangle-down.svg", Mode: ButtonMode.Repeat, Color: "gold"));

    private static readonly GridBlock LxOpacity = new("lx-opacity", "LX opacity",
        new OpacityButtonDef(0, 0, "LX more visible", "icons/plus.svg", Step, "gold"),
        new OpacityInfoTileDef(1, 0, Color: "gold"),
        new OpacityButtonDef(2, 0, "LX less visible", "icons/minus.svg", -Step, "gold"));

    private static readonly GridBlock LxAltimeter = new("lx-altimeter", "Altimeter / Vario",
        new ButtonDef(0, 0, "Equals", "Altimeter up", "=", "icons/arrow-triangle-up.svg", Mode: ButtonMode.Repeat, Color: "gold"),
        new ButtonDef(1, 0, "Minus", "Altimeter down", "-", "icons/arrow-triangle-down.svg", Mode: ButtonMode.Repeat, Color: "gold"),
        new ButtonDef(0, 1, "RightBracket", "Vario volume up", "]", "icons/arrow-triangle-up.svg", Mode: ButtonMode.Repeat, Color: "gold"),
        new ButtonDef(1, 1, "LeftBracket", "Vario volume down", "[", "icons/arrow-triangle-down.svg", Mode: ButtonMode.Repeat, Color: "gold"),
        new ButtonDef(2, 1, "RightCtrl", "Toggle<br>Climb / Cruise", "Right Ctrl", "icons/refresh.svg", Color: "gold"));

    // System volume (media keys)
    private static readonly GridBlock LxVolume = new("lx-volume", "System volume",
        new ButtonDef(0, 0, "VolumeUp", "System volume up", "Vol +", "icons/plus.svg", Mode: ButtonMode.Repeat, Color: "deepskyblue"),
        new ButtonDef(1, 0, "VolumeDown", "System volume down", "Vol -", "icons/minus.svg", Mode: ButtonMode.Repeat, Color: "deepskyblue"),
        new ButtonDef(2, 0, "VolumeMute", "System mute", "Mute", "icons/speaker-mute.svg", Color: "deepskyblue"));

    // Esc/Pause/Flow
    private static readonly GridBlock LxFlow = new("lx-flow", "Menu / Start / Pause",
        new ButtonDef(0, 0, "Escape", "Game menu", "Esc", "icons/menu.svg"),
        new ButtonDef(0, 1, "Shift", "Start flight", "Left Shift", "icons/start.svg", Color: "limegreen"),
        new ButtonDef(0, 2, "P", "Pause/Autopilot", "P", "icons/pause.svg"));

    private static readonly GridBlock LxChat = new("lx-chat", "Chat",
        new ButtonDef(0, 0, "Backspace", "Send message", "Backspace", "icons/send.svg"),
        new ButtonDef(0, 1, "D", "Extend chat log", "D", "icons/chat.svg"));

    private static readonly GridBlock LxBlue = new("lx-blue", "Release / Smoke / Gear",
        new ButtonDef(0, 0, "R", "Release", "R", "icons/disconnect.svg", Color: "deepskyblue"),
        new ButtonDef(0, 1, "T", "Smoke", "T", "icons/air.svg", Color: "deepskyblue"),
        new ButtonDef(0, 2, "D0", "G Meter reset", "0", "icons/refresh.svg", Color: "deepskyblue"),
        new ButtonDef(0, 3, "E", "Check time", "E", "icons/time.svg", Color: "deepskyblue"),
        new ButtonDef(1, 0, "G", "Landing Gear", "G", "icons/wheel.svg", Color: "deepskyblue"),
        new ButtonDef(1, 1, "W", "Water", "W", "icons/drop.svg", Color: "deepskyblue"),
        new ButtonDef(1, 2, "Comma", "Bug wipers", ",", "icons/wiper.svg", Color: "deepskyblue"),
        new ButtonDef(1, 3, "LeftCtrl", "Swap controls", "Left Ctrl", "icons/swap.svg", Color: "deepskyblue"));

    private static readonly GridBlock LxHelpers = new("lx-helpers", "Helpers",
        new ButtonDef(0, 0, "Q", "Miracle", "Q", "icons/miracle.svg", Color: "violet"),
        new ButtonDef(0, 1, "A", "Auto rudder toggle", "A", "icons/rudder.svg", Color: "violet"),
        new ButtonDef(1, 0, "H", "Lift helpers", "H", "icons/lift.svg", Color: "violet"),
        new ButtonDef(1, 1, "J", "Task helpers", "J", "icons/task.svg", Color: "violet"));

    // Cameras
    private static readonly GridBlock CamView = new("cam-view", "View control",
        new ButtonDef(0, 0, "NumPadAdd", "Zoom in", "Num +", "icons/zoom-in.svg", Color: "gold"),
        new ButtonDef(0, 1, "Up", "View up", "Up", "icons/arrow-up.svg"),
        new ButtonDef(0, 2, "NumPadSubtract", "Zoom out", "Num -", "icons/zoom-out.svg", Color: "gold"),
        new ButtonDef(1, 0, "Left", "View left", "Left", "icons/arrow-left.svg"),
        new ButtonDef(1, 1, "NumPad0", "View center", "Num 0", "icons/center.svg"),
        new ButtonDef(1, 2, "Right", "View right", "Right", "icons/arrow-right.svg"),
        new ButtonDef(2, 0, "NumPadDivide", "View snap left", "Num /", "icons/snap-left.svg", Color: "gold"),
        new ButtonDef(2, 1, "Down", "View down", "Down", "icons/arrow-down.svg"),
        new ButtonDef(2, 2, "NumPadMultiply", "View snap right", "Num *", "icons/snap-right.svg", Color: "gold"));

    private static readonly GridBlock CamPanel = new("cam-panel", "VR / Panel",
        new ButtonDef(0, 0, "F11", "VR center", "F11", "icons/vr.svg"),
        new ButtonDef(1, 0, "Y", "Panel zoom", "Y", "icons/panel-zoom.svg"));

    private static readonly GridBlock CamSelect = new("cam-select", "Camera select",
        new ButtonDef(0, 0, "F1", "Cockpit camera", "F1", "icons/cockpit.svg", Color: "deepskyblue"),
        new ButtonDef(0, 1, "Control+F1", "Cockpit visibility", "Ctrl + F1", "icons/eye.svg", Color: "deepskyblue"),
        new ButtonDef(0, 2, "F2", "External camera", "F2", "icons/camera.svg", Color: "deepskyblue"),
        new ButtonDef(0, 3, "F3", "Chase camera", "F3", "icons/video.svg", Color: "deepskyblue"),
        new ButtonDef(0, 4, "F4", "Tower camera", "F4", "icons/tower.svg", Color: "deepskyblue"),
        new ButtonDef(1, 0, "F5", "Towplane camera", "F5", "icons/towplane.svg", Color: "deepskyblue"),
        new ButtonDef(1, 1, "F6", "Fly-by camera", "F6", "icons/flyby.svg", Color: "deepskyblue"),
        new ButtonDef(1, 2, "F7", "Padlock camera", "F7", "icons/padlock.svg", Color: "deepskyblue"),
        new ButtonDef(1, 3, "F8", "Net player camera", "F8", "icons/network.svg", Color: "deepskyblue"),
        new ButtonDef(1, 4, "F9", "Replay Camera", "F9", "icons/replay.svg", Color: "deepskyblue"));

    private static readonly GridBlock CamMotor = new("cam-motor", "Motor",
        new ButtonDef(0, 0, "K", "Extract motor", "K", "icons/propeller-extract.svg", Color: "limegreen"),
        new ButtonDef(0, 1, "L", "Start motor", "L", "icons/propeller.svg", Color: "limegreen"),
        new ButtonDef(1, 1, "O", "Throttle up", "O", "icons/plus.svg", Mode: ButtonMode.Repeat, Color: "limegreen"),
        new ButtonDef(1, 0, "I", "Throttle down", "I", "icons/minus.svg", Mode: ButtonMode.Repeat, Color: "limegreen"));

    private static readonly GridBlock CamRadio = new("cam-radio", "Radio",
        new ButtonDef(0, 0, "Backslash", "Freq up", "\\", "icons/plus.svg", Mode: ButtonMode.Repeat),
        new ButtonDef(1, 0, "Quote", "Freq down", "'", "icons/minus.svg", Mode: ButtonMode.Repeat),
        new ButtonDef(0, 1, "D4", "Radio volume up", "4", "icons/plus.svg", Mode: ButtonMode.Repeat),
        new ButtonDef(1, 1, "D3", "Radio volume down", "3", "icons/minus.svg", Mode: ButtonMode.Repeat),
        new ButtonDef(0, 2, "D2", "Microphone volume up", "2", "icons/plus.svg", Mode: ButtonMode.Repeat),
        new ButtonDef(1, 2, "D1", "Microphone volume down", "1", "icons/minus.svg", Mode: ButtonMode.Repeat),
        new ButtonDef(2, 1, "Space", "Push To Talk", "Space", "icons/microphone.svg", Color: "deepskyblue"));

    // Display/misc (from Game Controls)
    private static readonly GridBlock CamMisc = new("cam-misc", "Display / Screenshot",
        new ButtonDef(0, 0, "Tab", "Show classification", "Tab", "icons/list.svg", Color: "hotpink"),
        new ButtonDef(0, 1, "Semicolon", "Show icons", ";", "icons/grid.svg", Color: "hotpink"),
        new ButtonDef(0, 2, "Grave", "HUD toggle", "`", "icons/hud.svg", Color: "hotpink"),
        new ButtonDef(0, 3, "S", "Screenshot", "S", "icons/screenshot.svg", Color: "hotpink"),
        new ButtonDef(0, 4, "Shift+D", "Show frame rate (FPS)", "Shift + D", "icons/fps.svg", Color: "hotpink"));

    // Airbrakes / Trimmer / Flaps
    private static readonly GridBlock GameFlight = new("game-flight", "Airbrakes / Trimmer / Flaps",
        new ButtonDef(0, 2, "N", "AirBrakes up", "N", "icons/arrow-triangle-up.svg", Color: "orange"),
        new ButtonDef(1, 2, "B", "AirBrakes down", "B", "icons/arrow-triangle-down.svg", Color: "orange"),
        new ButtonDef(0, 0, "Delete", "Trimmer up", "Delete", "icons/arrow-triangle-up.svg", Color: "gold"),
        new ButtonDef(2, 0, "Insert", "Trimmer down", "Insert", "icons/arrow-triangle-down.svg", Color: "gold"),
        new ButtonDef(1, 0, "F12", "Trimmer center", "F12", "icons/ok-circle.svg", Color: "gold"),
        new ButtonDef(0, 1, "F", "Flaps up", "F", "icons/arrow-triangle-up.svg", Color: "gold"),
        new ButtonDef(1, 1, "V", "Flaps down", "V", "icons/arrow-triangle-down.svg", Color: "gold"));

    /// <summary>All blocks in the default order.</summary>
    public static readonly IReadOnlyList<GridBlock> All =
    [
        LxPad, LxTurnpoint, LxFlightInfo, LxMacCready, LxOpacity, LxAltimeter, LxVolume, LxFlow, LxChat, LxBlue, LxHelpers,
        CamView, CamPanel, CamSelect, CamMotor, CamRadio, CamMisc,
        GameFlight
    ];

    public static readonly IReadOnlyDictionary<string, GridBlock> ById = All.ToDictionary(b => b.Id);

    /// <summary>Throws on duplicate ids and overlapping items inside a block; warns about blocks larger than 3x3.</summary>
    public static void Validate()
    {
        var duplicate = All.GroupBy(b => b.Id).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Block id '{duplicate.Key}' is used more than once.");
        }

        foreach (var block in All)
        {
            // Items on the same cell are stacked by the CSS grid without any error.
            var overlap = block.Items.GroupBy(i => (i.Row, i.Col)).FirstOrDefault(g => g.Count() > 1);
            if (overlap is not null)
            {
                throw new InvalidOperationException(
                    $"Block '{block.Id}': row {overlap.Key.Row}, col {overlap.Key.Col} is used by " + string.Join(" and ", overlap.Select(Describe)));
            }

            if (block.Width > MaxRecommendedSize || block.Height > MaxRecommendedSize)
            {
                Console.WriteLine($"[BlockCatalog] Block '{block.Id}' is {block.Width}x{block.Height} and should be split into blocks of at most {MaxRecommendedSize}x{MaxRecommendedSize}.");
            }
        }
    }

    private static string Describe(GridItemDef item) => item switch
    {
        ButtonDef b => $"button '{b.Tag}' ({b.Label})",
        InfoTileDef t => $"info tile '{t.Label}'",
        OpacityButtonDef o => $"opacity button '{o.Label}'",
        OpacityInfoTileDef o => $"opacity tile '{o.Label}'",
        _ => item.GetType().Name
    };
}
