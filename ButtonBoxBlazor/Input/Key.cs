using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

public static partial class Keyboard
{
    //https://www.marjorie.de/ps2/scancode-set1.htm

    /// ---     ---------------   ---------------   ---------------   -----------
    /// | 01|   | 3B| 3C| 3D| 3E| | 3F| 40| 41| 42| | 43| 44| 57| 58| |+37|+46|+45| 
    ///  ---     ---------------   ---------------   ---------------   -----------
    ///  -----------------------------------------------------------   -----------   ---------------
    /// | 29| 02| 03| 04| 05| 06| 07| 08| 09| 0A| 0B| 0C| 0D|     0E| |*52|*47|*49| |+45|+35|+37| 4A|
    /// |-----------------------------------------------------------| |-----------| |---------------|
    /// |   0F| 10| 11| 12| 13| 14| 15| 16| 17| 18| 19| 1A| 1B|     | |*53|*4F|*51| | 47| 48| 49|   |
    /// |------------------------------------------------------|  1C|  -----------  |-----------| 4E|
    /// |    3A| 1E| 1F| 20| 21| 22| 23| 24| 25| 26| 27| 28|!2B|    |               | 4B| 4C| 4D|   |
    /// |-----------------------------------------------------------|      ---      |---------------|
    /// |  2A| 56| 2C| 2D| 2E| 2F| 30| 31| 32| 33| 34| 35|        36|     |*48|     | 4F| 50| 51|   |
    /// |-----------------------------------------------------------|  -----------  |-----------|-1C|
    /// |   1D|-5B|   38|                       39|-38|-5C|-5D|  -1D| |*4B|*50|*4D| |     52| 53|   |
    ///  -----------------------------------------------------------   -----------   ---------------

    /// <summary>
    /// A scan code (PS/2 Set 1) plus its extended-key flag. Several keys share the same base
    /// scan code (e.g. Insert/NumPad0) and are only distinguishable via the extended-key flag,
    /// so both parts are needed to identify a key uniquely; comparing a bare scan-code value
    /// (as a plain enum would) cannot tell them apart.
    /// <see cref="Name"/> defaults to the declaring field's name via <see cref="CallerMemberNameAttribute"/>,
    /// which both doubles as the tag string used by <see cref="TryParse"/> and shows up in the
    /// auto-generated ToString(), making a key self-describing for debugging.
    /// </summary>
    public readonly record struct Key(ushort ScanCode, bool Extended = false, [CallerMemberName] string Name = "")
    {
        public static readonly Key A = new(0x1E);
        public static readonly Key B = new(0x30);
        public static readonly Key C = new(0x2E);
        public static readonly Key D = new(0x20);
        public static readonly Key E = new(0x12);
        public static readonly Key F = new(0x21);
        public static readonly Key G = new(0x22);
        public static readonly Key H = new(0x23);
        public static readonly Key I = new(0x17);
        public static readonly Key J = new(0x24);
        public static readonly Key K = new(0x25);
        public static readonly Key L = new(0x26);
        public static readonly Key M = new(0x32);
        public static readonly Key N = new(0x31);
        public static readonly Key O = new(0x18);
        public static readonly Key P = new(0x19);
        public static readonly Key Q = new(0x10);
        public static readonly Key R = new(0x13);
        public static readonly Key S = new(0x1F);
        public static readonly Key T = new(0x14);
        public static readonly Key U = new(0x16);
        public static readonly Key V = new(0x2F);
        public static readonly Key W = new(0x11);
        public static readonly Key X = new(0x2D);
        public static readonly Key Y = new(0x15);
        public static readonly Key Z = new(0x2C);

        public static readonly Key D0 = new(0x0B);
        public static readonly Key D1 = new(0x02);
        public static readonly Key D2 = new(0x03);
        public static readonly Key D3 = new(0x04);
        public static readonly Key D4 = new(0x05);
        public static readonly Key D5 = new(0x06);
        public static readonly Key D6 = new(0x07);
        public static readonly Key D7 = new(0x08);
        public static readonly Key D8 = new(0x09);
        public static readonly Key D9 = new(0x0A);

        public static readonly Key Space = new(0x39);
        public static readonly Key Enter = new(0x1C);
        public static readonly Key Escape = new(0x01);
        public static readonly Key Tab = new(0x0F);
        public static readonly Key Backspace = new(0x0E);
        public static readonly Key CapsLock = new(0x3A);

        public static readonly Key Minus = new(0x0C);
        // Named EqualsSign, not Equals, to avoid clashing with the record struct's generated Equals method;
        // Name is pinned to "Equals" explicitly since CallerMemberName would otherwise capture "EqualsSign".
        public static readonly Key EqualsSign = new(0x0D, Name: "Equals");
        public static readonly Key Comma = new(0x33);
        public static readonly Key Period = new(0x34);
        public static readonly Key Semicolon = new(0x27);
        public static readonly Key Quote = new(0x28);
        public static readonly Key LeftBracket = new(0x1A);
        public static readonly Key RightBracket = new(0x1B);
        public static readonly Key Backslash = new(0x2B);
        public static readonly Key Grave = new(0x29);
        // ISO-only key between LeftShift and Z (DE/CH: "< >", UK: "\|"); absent on US ANSI keyboards.
        public static readonly Key Iso102 = new(0x56);

        public static readonly Key F1 = new(0x3B);
        public static readonly Key F2 = new(0x3C);
        public static readonly Key F3 = new(0x3D);
        public static readonly Key F4 = new(0x3E);
        public static readonly Key F5 = new(0x3F);
        public static readonly Key F6 = new(0x40);
        public static readonly Key F7 = new(0x41);
        public static readonly Key F8 = new(0x42);
        public static readonly Key F9 = new(0x43);
        public static readonly Key F10 = new(0x44);
        public static readonly Key F11 = new(0x57);
        public static readonly Key F12 = new(0x58);

        // Share their base scan code with the numpad keys below; the extended-key flag
        // resolves them to arrow keys instead of numpad digits.
        public static readonly Key Left = new(0x4B, Extended: true);
        public static readonly Key Up = new(0x48, Extended: true);
        public static readonly Key Right = new(0x4D, Extended: true);
        public static readonly Key Down = new(0x50, Extended: true);

        // Share their base scan code with the numpad keys below; the extended-key flag
        // resolves them to Insert/Delete instead of numpad digits.
        public static readonly Key Insert = new(0x52, Extended: true);
        public static readonly Key Delete = new(0x53, Extended: true);

        public static readonly Key Shift = new(0x2A);
        public static readonly Key Control = new(0x1D);
        public static readonly Key Alt = new(0x38);

        public static readonly Key LeftCtrl = new(0x1D);
        // Shares its base scan code with LeftCtrl; distinguished via the extended-key flag.
        public static readonly Key RightCtrl = new(0x1D, Extended: true);
        public static readonly Key RightShift = new(0x36);
        public static readonly Key LeftAlt = new(0x38);
        // Shares its base scan code with LeftAlt; distinguished via the extended-key flag.
        public static readonly Key RightAlt = new(0x38, Extended: true);

        // Windows/Menu keys only exist as extended keys.
        public static readonly Key LeftWindows = new(0x5B, Extended: true);
        public static readonly Key RightWindows = new(0x5C, Extended: true);
        public static readonly Key Menu = new(0x5D, Extended: true);

        // Distinguished from the Pause-sequence's shared byte via the extended-key flag.
        public static readonly Key NumLock = new(0x45, Extended: true);
        public static readonly Key ScrollLock = new(0x46);

        // Home/End/Prior/Next share base codes with the numpad keys below; the extended-key
        // flag resolves them to the correct key.
        public static readonly Key Home = new(0x47, Extended: true);
        public static readonly Key End = new(0x4F, Extended: true);
        public static readonly Key Prior = new(0x49, Extended: true);
        public static readonly Key Next = new(0x51, Extended: true);

        public static readonly Key NumPad0 = new(0x52);
        public static readonly Key NumPad1 = new(0x4F);
        public static readonly Key NumPad2 = new(0x50);
        public static readonly Key NumPad3 = new(0x51);
        public static readonly Key NumPad4 = new(0x4B);
        public static readonly Key NumPad5 = new(0x4C);
        public static readonly Key NumPad6 = new(0x4D);
        public static readonly Key NumPad7 = new(0x47);
        public static readonly Key NumPad8 = new(0x48);
        public static readonly Key NumPad9 = new(0x49);
        public static readonly Key NumPadDecimal = new(0x53);
        public static readonly Key NumPadAdd = new(0x4E);
        public static readonly Key NumPadSubtract = new(0x4A);
        public static readonly Key NumPadMultiply = new(0x37);
        // Shares its base scan code with NumPadDivide; distinguished via the extended-key flag.
        public static readonly Key Slash = new(0x35);
        // Shares its base scan code with the main keyboard's "/" key; distinguished via the extended-key flag.
        public static readonly Key NumPadDivide = new(0x35, Extended: true);
        // Shares its base scan code with Enter; distinguished via the extended-key flag.
        public static readonly Key NumPadEnter = new(0x1C, Extended: true);

        private static readonly Dictionary<string, Key> ByName = BuildLookup();

        // Replaces Enum.TryParse: looks a key up by its Name (used by the button-tag parser).
        public static bool TryParse(string name, out Key key) => ByName.TryGetValue(name, out key);

        private static Dictionary<string, Key> BuildLookup()
        {
            var map = new Dictionary<string, Key>();

            foreach (var field in typeof(Key).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(Key))
                {
                    var key = (Key)field.GetValue(null)!;
                    map[key.Name] = key;
                }
            }

            return map;
        }
    }
}
