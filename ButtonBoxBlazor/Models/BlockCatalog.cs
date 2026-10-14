using System.Globalization;

/// <summary>
/// All available blocks in their default order. Item positions inside a block are relative to its origin;
/// the <c>LayoutPacker</c> decides where a block ends up.
/// </summary>
public static class BlockCatalog {
  public const int MaxRecommendedSize = 3;

  private const int Step = 10;

  // Blocks below must stay above All (static initializers run in textual order).

  // LX Pad 3x3
  private static readonly GridBlock LxPad = new("lx-pad", "LX Pad", "white",
      new ButtonDef(0, 1, "NumPad8", "LX-Stick up", "Num 8", "icons/arrow-up.svg", Function: 3036),
      new ButtonDef(0, 0, "NumPad7", "Previous mode", "Num 7", "icons/arrow-double-left.svg"),
      new ButtonDef(1, 0, "NumPad4", "LX-Stick left<br>Zoom out", "Num 4", "icons/arrow-left.svg"),
      new ButtonDef(1, 1, "NumPad5", "OK", "Num 5", "icons/ok-circle.svg", Color: "limegreen", Function: 3038),
      new ButtonDef(2, 1, "NumPad2", "LX-Stick down", "Num 2", "icons/arrow-down.svg", Function: 3037),
      new ButtonDef(1, 2, "NumPad6", "LX-Stick right<br>Zoom in", "Num 6", "icons/arrow-right.svg"),
      new ButtonDef(2, 2, "NumPad3", "Abort", "Num 3", "icons/abort-cross.svg", Color: "red", Function: 3039),
      new ButtonDef(0, 2, "NumPad9", "Next mode", "Num 9", "icons/arrow-double-right.svg"));

  // LX Turnpoint 2x2
  private static readonly GridBlock Turnpoint = new("lx-turnpoint", "LX Turnpoint", "turquoise",
      new ButtonDef(0, 1, "Prior", "Info Next<br>Turnpoint", "Page Up", "icons/arrow-triangle-up.svg", Function: 3042),
      new ButtonDef(1, 1, "Next", "Info Previous<br>Turnpoint", "Page Down", "icons/arrow-triangle-down.svg", Function: 3043),
      new ButtonDef(0, 0, "Slash", "Switch AAT<br>Turnpoint", "/", "icons/arrow-double-right.svg", Function: 3044),
      new ButtonDef(1, 0, "RightShift", "AAT-Turnpoint Shift On/Off", "Right Shift", "icons/arrow-four-way.svg", Function: 3045));

  // Flight info 3x1
  private static readonly GridBlock FlightInfo = new("lx-flight-info", "Flight info", "lightsteelblue",
      new InfoTileDef(0, 0, "Time", t => TimeSpan.FromHours(t.Time % 24).ToString(@"hh\:mm\:ss")),
      new InfoTileDef(1, 0, "Water", t => t.Water.ToString("F0", CultureInfo.InvariantCulture) + " kg"),
      new InfoTileDef(2, 0, "Above ground", t => t.Height.ToString("F0", CultureInfo.InvariantCulture) + " m"));

  // MacCready 3x1
  private static readonly GridBlock MacCready = new("lx-mac-cready", "MacCready", "gold",
      new ButtonDef(0, 0, "Home", "Increase MacCready", "Home", "icons/arrow-triangle-up.svg", Mode: ButtonMode.Repeat, Function: 3046),
      new InfoTileDef(1, 0, "MacCready m/s", t => t.MacCready.ToString("F2", CultureInfo.InvariantCulture)),
      new ButtonDef(2, 0, "End", "Decrease MacCready", "End", "icons/arrow-triangle-down.svg", Mode: ButtonMode.Repeat, Function: 3047));

  // LX opacity 3x1
  private static readonly GridBlock LxOpacity = new("lx-opacity", "LX opacity", "silver",
      new OpacityButtonDef(0, 0, "LX more visible", "icons/plus.svg", Step),
      new OpacityInfoTileDef(1, 0),
      new OpacityButtonDef(2, 0, "LX less visible", "icons/minus.svg", -Step));

  // Altimeter 2x1
  private static readonly GridBlock Altimeter = new("lx-altimeter", "Altimeter", "orange",
      new ButtonDef(0, 0, "Equals", "Altimeter up", "=", "icons/arrow-triangle-up.svg", Mode: ButtonMode.Repeat, Function: 3028),
      new ButtonDef(1, 0, "Minus", "Altimeter down", "-", "icons/arrow-triangle-down.svg", Mode: ButtonMode.Repeat, Function: 3029));

  // Vario 3x1
  private static readonly GridBlock VarioVolume = new("lx-vario", "Vario", "mediumpurple",
      new ButtonDef(0, 0, "RightBracket", "Vario volume up", "]", "icons/arrow-triangle-up.svg", Mode: ButtonMode.Repeat, Function: 3026),
      new ButtonDef(1, 0, "LeftBracket", "Vario volume down", "[", "icons/arrow-triangle-down.svg", Mode: ButtonMode.Repeat, Function: 3027),
      new ButtonDef(2, 0, "RightCtrl", "Toggle<br>Climb / Cruise", "Right Ctrl", "icons/refresh.svg", Function: 3048));

  // System volume 3x1
  private static readonly GridBlock SystemVolume = new("System-volume", "System volume", "deepskyblue",
      new ButtonDef(0, 0, "VolumeUp", "System volume up", "Vol +", "icons/plus.svg", Mode: ButtonMode.Repeat),
      new ButtonDef(1, 0, "VolumeDown", "System volume down", "Vol -", "icons/minus.svg", Mode: ButtonMode.Repeat),
      new ButtonDef(2, 0, "VolumeMute", "System mute", "Mute", "icons/speaker-mute.svg"));

  // Menu / Start / Pause 3x1
  private static readonly GridBlock Menu = new("lx-flow", "Menu / Start / Pause", "palegreen",
      new ButtonDef(0, 0, "Escape", "Game menu", "Esc", "icons/menu.svg", Function: 3069),
      new ButtonDef(1, 0, "Shift", "Start flight", "Left Shift", "icons/start.svg", Color: "limegreen", Function: 3070),
      new ButtonDef(2, 0, "P", "Pause/Autopilot", "P", "icons/pause.svg", Function: 3071));

  // Chat 2x1
  private static readonly GridBlock Chat = new("lx-chat", "Chat", "wheat",
      new ButtonDef(0, 0, "Backspace", "Send message", "Backspace", "icons/send.svg", Function: 3074),
      new ButtonDef(1, 0, "D", "Extend chat log", "D", "icons/chat.svg", Function: 3078));

  // Release / Smoke / Gear 3x3
  private static readonly GridBlock Release = new("Release", "Release / Smoke / Gear", "#a0a0ff",
      new ButtonDef(0, 0, "R", "Release", "R", "icons/disconnect.svg", Function: 3016),
      new ButtonDef(0, 1, "T", "Smoke", "T", "icons/air.svg", Function: 3018),
      new ButtonDef(0, 2, "D0", "G Meter reset", "0", "icons/refresh.svg", Function: 3030),
      new ButtonDef(1, 0, "E", "Check time", "E", "icons/time.svg", Function: 3024),
      new ButtonDef(1, 1, "G", "Landing Gear", "G", "icons/wheel.svg"),
      new ButtonDef(1, 2, "W", "Water", "W", "icons/drop.svg", Function: 3017),
      new ButtonDef(2, 0, "Comma", "Bug wipers", ",", "icons/wiper.svg", Function: 3023),
      new ButtonDef(2, 1, "LeftCtrl", "Swap controls", "Left Ctrl", "icons/swap.svg", Function: 3025));

  // Helpers 2x2
  private static readonly GridBlock Helpers = new("helpers", "Helpers", "plum",
      new ButtonDef(0, 0, "Q", "Miracle", "Q", "icons/miracle.svg", Function: 3072),
      new ButtonDef(0, 1, "A", "Auto rudder toggle", "A", "icons/rudder.svg", Function: 3073),
      new ButtonDef(1, 0, "H", "Lift helpers", "H", "icons/lift.svg", Function: 3080),
      new ButtonDef(1, 1, "J", "Task helpers", "J", "icons/task.svg", Function: 3081));

  // View control 3x3
  private static readonly GridBlock CamView = new("cam-view", "View control", "yellowgreen",
      new ButtonDef(0, 0, "NumPadAdd", "Zoom in", "Num +", "icons/zoom-in.svg", Color: "yellow", Function: 3066),
      new ButtonDef(0, 1, "Up", "View up", "Up", "icons/arrow-up.svg", Function: 3051),
      new ButtonDef(0, 2, "NumPadSubtract", "Zoom out", "Num -", "icons/zoom-out.svg", Color: "yellow", Function: 3067),
      new ButtonDef(1, 0, "Left", "View left", "Left", "icons/arrow-left.svg", Function: 3049),
      new ButtonDef(1, 1, "NumPad0", "View center", "Num 0", "icons/center.svg", Function: 3055),
      new ButtonDef(1, 2, "Right", "View right", "Right", "icons/arrow-right.svg", Function: 3050),
      new ButtonDef(2, 0, "NumPadDivide", "View snap left", "Num /", "icons/snap-left.svg", Color: "yellow", Function: 3053),
      new ButtonDef(2, 1, "Down", "View down", "Down", "icons/arrow-down.svg", Function: 3052),
      new ButtonDef(2, 2, "NumPadMultiply", "View snap right", "Num *", "icons/snap-right.svg", Color: "yellow", Function: 3054));

  // VR / Panel 2x1
  private static readonly GridBlock CamPanel = new("cam-panel", "VR / Panel", "peru",
      new ButtonDef(0, 0, "F11", "VR center", "F11", "icons/vr.svg", Function: 3056),
      new ButtonDef(1, 0, "Y", "Panel zoom", "Y", "icons/panel-zoom.svg", Function: 3068));

  // Camera select 3x4
  private static readonly GridBlock CamSelect = new("cam-select", "Camera select", "hotpink",
      new ButtonDef(0, 0, "F1", "Cockpit camera", "F1", "icons/cockpit.svg", Function: 3057),
      new ButtonDef(0, 1, "Control+F1", "Cockpit visibility", "Ctrl + F1", "icons/eye.svg"),
      new ButtonDef(0, 2, "F2", "External camera", "F2", "icons/camera.svg", Function: 3058),
      new ButtonDef(0, 3, "F3", "Chase camera", "F3", "icons/video.svg", Function: 3059),
      new ButtonDef(1, 0, "F4", "Tower camera", "F4", "icons/tower.svg", Function: 3060),
      new ButtonDef(1, 1, "F5", "Towplane camera", "F5", "icons/towplane.svg", Function: 3061),
      new ButtonDef(1, 2, "F6", "Fly-by camera", "F6", "icons/flyby.svg", Function: 3062),
      new ButtonDef(1, 3, "F7", "Padlock camera", "F7", "icons/padlock.svg", Function: 3063),
      new ButtonDef(2, 0, "F8", "Net player camera", "F8", "icons/network.svg", Function: 3064),
      new ButtonDef(2, 1, "F9", "Replay Camera", "F9", "icons/replay.svg", Function: 3065));

  // Motor 2x2
  private static readonly GridBlock Motor = new("cam-motor", "Motor", "limegreen",
      new ButtonDef(0, 0, "K", "Extract motor", "K", "icons/propeller-extract.svg", Function: 3019),
      new ButtonDef(0, 1, "L", "Start motor", "L", "icons/propeller.svg", Function: 3020),
      new ButtonDef(1, 1, "O", "Throttle up", "O", "icons/plus.svg", Mode: ButtonMode.Repeat, Function: 3021),
      new ButtonDef(1, 0, "I", "Throttle down", "I", "icons/minus.svg", Mode: ButtonMode.Repeat, Function: 3022));

  // Radio 3x3
  private static readonly GridBlock Radio = new("cam-radio", "Radio", "mediumaquamarine",
      new ButtonDef(0, 0, "Backslash", "Freq up", "\\", "icons/plus.svg", Mode: ButtonMode.Repeat, Function: 3031),
      new ButtonDef(1, 0, "Quote", "Freq down", "'", "icons/minus.svg", Mode: ButtonMode.Repeat, Function: 3032),
      new ButtonDef(0, 1, "D4", "Radio volume up", "4", "icons/plus.svg", Mode: ButtonMode.Repeat),
      new ButtonDef(1, 1, "D3", "Radio volume down", "3", "icons/minus.svg", Mode: ButtonMode.Repeat),
      new ButtonDef(0, 2, "D2", "Microphone volume up", "2", "icons/plus.svg", Mode: ButtonMode.Repeat),
      new ButtonDef(1, 2, "D1", "Microphone volume down", "1", "icons/minus.svg", Mode: ButtonMode.Repeat),
      new ButtonDef(2, 1, "Space", "Push To Talk", "Space", "icons/microphone.svg", Color: "deepskyblue", Function: 3033));

  // Display / Screenshot 3x2
  private static readonly GridBlock CamMisc = new("cam-misc", "Display / Screenshot", "salmon",
      new ButtonDef(0, 0, "Tab", "Show classification", "Tab", "icons/list.svg", Function: 3075),
      new ButtonDef(0, 1, "Semicolon", "Show icons", ";", "icons/grid.svg", Function: 3076),
      new ButtonDef(1, 0, "Grave", "HUD toggle", "`", "icons/hud.svg", Function: 3077),
      new ButtonDef(1, 1, "S", "Screenshot", "S", "icons/screenshot.svg", Function: 3079),
      new ButtonDef(2, 0, "Shift+D", "Show frame rate (FPS)", "Shift + D", "icons/fps.svg"));

  // Airbrakes / Trimmer / Flaps 3x3
  private static readonly GridBlock TrimmerFlaps = new("game-flight", "Trimmer / Flaps", "tomato",

      new ButtonDef(0, 0, "Delete", "Trimmer up", "Delete", "icons/arrow-triangle-up.svg"),
      new ButtonDef(2, 0, "Insert", "Trimmer down", "Insert", "icons/arrow-triangle-down.svg"),
      new ButtonDef(1, 0, "F12", "Trimmer center", "F12", "icons/ok-circle.svg", Function: 3009),
      new ButtonDef(0, 1, "F", "Flaps up", "F", "icons/arrow-triangle-up.svg"),
      new ButtonDef(1, 1, "V", "Flaps down", "V", "icons/arrow-triangle-down.svg"));

  private static readonly GridBlock AirBrakes = new("AirBrakes", "Airbrakes", "yellow",
      new ButtonDef(0, 0, "N", "AirBrakes up", "N", "icons/arrow-triangle-up.svg"),
      new ButtonDef(1, 0, "B", "AirBrakes down", "B", "icons/arrow-triangle-down.svg"));

  /// <summary>All blocks in the default order.</summary>
  public static readonly IReadOnlyList<GridBlock> All =
  [
    LxPad,
    FlightInfo,
    Menu,
    LxOpacity,
    SystemVolume,
    VarioVolume,
    MacCready,
    Release,
    Turnpoint,
    Helpers,
    Chat,
    CamPanel,
    CamMisc,
    Altimeter,
    CamSelect,
    Motor,
    CamView,
    TrimmerFlaps,
    AirBrakes,
    Radio
  ];

  public static readonly IReadOnlyDictionary<string, GridBlock> ById = All.ToDictionary(b => b.Id);

  /// <summary>Throws on duplicate ids and overlapping items inside a block; warns about blocks taller than 3 rows.</summary>
  public static void Validate() {
    var duplicate = All.GroupBy(b => b.Id).FirstOrDefault(g => g.Count() > 1);
    if(duplicate is not null) {
      throw new InvalidOperationException($"Block id '{duplicate.Key}' is used more than once.");
    }

    foreach(var block in All) {
      // Items on the same cell are stacked by the CSS grid without any error.
      var overlap = block.Items.GroupBy(i => (i.Row, i.Col)).FirstOrDefault(g => g.Count() > 1);
      if(overlap is not null) {
        throw new InvalidOperationException(
            $"Block '{block.Id}': row {overlap.Key.Row}, col {overlap.Key.Col} is used by " + string.Join(" and ", overlap.Select(Describe)));
      }

      if(block.Height > MaxRecommendedSize) {
        Console.WriteLine($"[BlockCatalog] Block '{block.Id}' is {block.Height}x{block.Width} (rows x columns) and should be split into blocks of at most {MaxRecommendedSize} rows.");
      }
    }
  }

  private static string Describe(GridItemDef item) => item switch {
    ButtonDef b => $"button '{b.Tag}' ({b.Label})",
    InfoTileDef t => $"info tile '{t.Label}'",
    OpacityButtonDef o => $"opacity button '{o.Label}'",
    OpacityInfoTileDef o => $"opacity tile '{o.Label}'",
    _ => item.GetType().Name
  };
}
