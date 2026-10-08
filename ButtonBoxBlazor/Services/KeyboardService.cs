using System.ComponentModel;
using System.Runtime.InteropServices;

public interface IKeyboardService
{
    /// <summary>
    /// Sends a key referenced by name (a <see cref="Key"/> name). Multiple names joined with '+'
    /// (e.g. "Control+F1") are sent as a modifier combo.
    /// </summary>
    void SendTag(string tag);

    /// <summary>
    /// Presses every key in the tag without releasing it (e.g. Push To Talk). Ignored while the tag is already held.
    /// </summary>
    void SendTagDown(string tag);

    /// <summary>
    /// Releases a tag pressed via <see cref="SendTagDown"/>. Ignored if the tag is not held.
    /// </summary>
    void SendTagUp(string tag);
}

/// <summary>
/// Injects keyboard input into the foreground application via Win32 SendInput; releases held keys on dispose.
/// </summary>
public sealed class KeyboardService : IKeyboardService, IDisposable
{
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_SCANCODE = 0x0008;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

    // Some games poll keyboard state on a fixed tick, so a zero-delay down/up burst for modifier combos can be missed.
    private const int ComboKeyDelayMs = 15;

    private readonly object _lock = new();
    private readonly HashSet<string> _heldTags = [];

    public void SendTag(string tag)
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

    public void SendTagDown(string tag)
    {
        // Guards against spurious pointerleave/cancel events releasing a key that was never pressed.
        lock (_lock)
        {
            if (!_heldTags.Add(tag))
            {
                return;
            }
        }

        if (!TryParseTag(tag, out var keys))
        {
            lock (_lock) _heldTags.Remove(tag);
            return;
        }

        PressAll(keys);
    }

    public void SendTagUp(string tag)
    {
        lock (_lock)
        {
            if (!_heldTags.Remove(tag))
            {
                return;
            }
        }

        if (TryParseTag(tag, out var keys))
        {
            ReleaseAll(keys);
        }
    }

    public void Dispose()
    {
        string[] held;
        lock (_lock)
        {
            held = [.. _heldTags];
            _heldTags.Clear();
        }

        foreach (var tag in held)
        {
            if (TryParseTag(tag, out var keys))
            {
                ReleaseAll(keys);
            }
        }
    }

    private static void PressAll(Key[] keys)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            SendKeyDown(keys[i]);
            if (i < keys.Length - 1) Thread.Sleep(ComboKeyDelayMs);
        }
    }

    private static void ReleaseAll(Key[] keys)
    {
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

    private static void SendKeyDown(Key key) =>
        SendInputKey(key.ScanCode, KEYEVENTF_SCANCODE | ExtendedFlag(key));

    private static void SendKeyUp(Key key) =>
        SendInputKey(key.ScanCode, KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP | ExtendedFlag(key));

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
