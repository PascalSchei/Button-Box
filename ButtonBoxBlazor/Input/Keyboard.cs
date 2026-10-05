using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

public static partial class Keyboard
{
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_SCANCODE = 0x0008;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

    // Some games poll keyboard state on a fixed tick instead of reacting to individual
    // events, so a zero-delay down/up burst for modifier combos (e.g. "Control+F1") can
    // be missed entirely even though the same combo works fine from a physical keyboard.
    private const int ComboKeyDelayMs = 15;

    /// <summary>
    /// Sends a key referenced by name, matching the button "Tag" convention used by the
    /// WPF Buttonbox app: a <see cref="Key"/> name. Multiple key names joined with '+'
    /// (e.g. "Control+F1") are sent as a modifier combo.
    /// </summary>
    public static void SendTag(string tag)
    {
        if (!TryParseTag(tag, out var keys))
        {
            return;
        }

        bool isCombo = keys.Length > 1;

        for (int i = 0; i < keys.Length - 1; i++)
        {
            SendKeyDown(keys[i]);
            if (isCombo) Thread.Sleep(ComboKeyDelayMs);
        }

        SendKeyDown(keys[^1]);
        if (isCombo) Thread.Sleep(ComboKeyDelayMs);
        SendKeyUp(keys[^1]);
        if (isCombo) Thread.Sleep(ComboKeyDelayMs);

        for (int i = keys.Length - 2; i >= 0; i--)
        {
            SendKeyUp(keys[i]);
            if (isCombo) Thread.Sleep(ComboKeyDelayMs);
        }
    }

    /// <summary>
    /// Presses down (without releasing) every key in the tag, outermost modifier first, for
    /// press/hold/release controls like "Push To Talk". Pair with <see cref="SendTagUp"/>.
    /// </summary>
    public static void SendTagDown(string tag)
    {
        if (!TryParseTag(tag, out var keys))
        {
            return;
        }

        for (int i = 0; i < keys.Length; i++)
        {
            SendKeyDown(keys[i]);
            if (i < keys.Length - 1) Thread.Sleep(ComboKeyDelayMs);
        }
    }

    /// <summary>
    /// Releases every key in the tag in reverse order. Pair with <see cref="SendTagDown"/>.
    /// </summary>
    public static void SendTagUp(string tag)
    {
        if (!TryParseTag(tag, out var keys))
        {
            return;
        }

        for (int i = keys.Length - 1; i >= 0; i--)
        {
            SendKeyUp(keys[i]);
            if (i > 0) Thread.Sleep(ComboKeyDelayMs);
        }
    }

    private static bool TryParseTag(string tag, out Key[] keys)
    {
        var parts = tag.Split('+');
        keys = new Key[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            if (!Key.TryParse(parts[i], out keys[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static void SendKey(Key key)
    {
        SendKeyDown(key);
        SendKeyUp(key);
    }

    public static void SendKeyDown(Key key)
    {        
        var flags = KEYEVENTF_SCANCODE | ExtendedFlag(key);
        //Console.WriteLine($"Sending key down: key={key.Name}, ScanCode=0x{key.ScanCode:X}, Extended={key.Extended}, Flags=0x{flags:X}");
        SendInputKey(key.ScanCode, flags);
    }

    public static void SendKeyUp(Key key)
    {
        var flags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP | ExtendedFlag(key);
        //Console.WriteLine($"Sending key up: key={key.Name}, ScanCode=0x{key.ScanCode:X}, Extended={key.Extended}, Flags=0x{flags:X}");
        SendInputKey(key.ScanCode, flags);
    }

    private static uint ExtendedFlag(Key key) =>
        key.Extended ? KEYEVENTF_EXTENDEDKEY : 0;

    private static void SendInputKey(ushort scanCode, uint flags)
    {
        INPUT input = new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = scanCode,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };

        uint result = SendInput(1, ref input, Marshal.SizeOf<INPUT>());

        if (result != 1)
        {
            int error = Marshal.GetLastWin32Error();
            throw new Win32Exception(error, $"SendInput fehlgeschlagen. Result={result}, Error={error}");
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, ref INPUT pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }
}
