using System.Diagnostics;
using System.Runtime.InteropServices;

/// <summary>
/// Changes the opacity of the main window of an external process (default: LXSim.exe).
/// </summary>
public sealed class WindowTransparencyService : IDisposable
{
    private const string ProcessName = "LXSim";
    private const int MinPercent = 10;
    private const int MaxPercent = 100;

    private const int GWL_EXSTYLE = -20;
    private const long WS_EX_LAYERED = 0x00080000;
    private const uint LWA_ALPHA = 0x2;

    private readonly object _lock = new();
    // Windows where WS_EX_LAYERED was set by this service; restored on dispose.
    private readonly HashSet<IntPtr> _layeredByUs = [];

    /// <summary>
    /// Changes the opacity by <paramref name="deltaPercent"/>. Returns the new opacity in percent, or an error message.
    /// </summary>
    public (int? Percent, string? Error) ChangeOpacity(int deltaPercent)
    {
        lock (_lock)
        {
            var handle = FindWindow();
            if (handle == IntPtr.Zero)
            {
                return (null, $"{ProcessName}.exe not found");
            }

            var exStyle = GetWindowLongPtr(handle, GWL_EXSTYLE).ToInt64();
            var percent = MaxPercent;
            if ((exStyle & WS_EX_LAYERED) != 0
                && GetLayeredWindowAttributes(handle, out _, out var current, out var flags)
                && (flags & LWA_ALPHA) != 0)
            {
                percent = (int)Math.Round(current * 100 / 255.0);
            }

            percent = Math.Clamp(percent + deltaPercent, MinPercent, MaxPercent);

            if ((exStyle & WS_EX_LAYERED) == 0)
            {
                Marshal.SetLastPInvokeError(0);
                if (SetWindowLongPtr(handle, GWL_EXSTYLE, new IntPtr(exStyle | WS_EX_LAYERED)) == IntPtr.Zero
                    && Marshal.GetLastPInvokeError() != 0)
                {
                    return (null, "Access denied (LXSim may be running as administrator)");
                }
                _layeredByUs.Add(handle);
            }

            var alpha = (byte)Math.Round(percent * 255 / 100.0);
            return SetLayeredWindowAttributes(handle, 0, alpha, LWA_ALPHA)
                ? (percent, null)
                : (null, "Could not set transparency");
        }
    }

    private static IntPtr FindWindow()
    {
        foreach (var process in Process.GetProcessesByName(ProcessName))
        {
            using (process)
            {
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    return process.MainWindowHandle;
                }
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var handle in _layeredByUs)
            {
                if (!IsWindow(handle))
                {
                    continue;
                }

                SetLayeredWindowAttributes(handle, 0, 255, LWA_ALPHA);
                var exStyle = GetWindowLongPtr(handle, GWL_EXSTYLE).ToInt64();
                SetWindowLongPtr(handle, GWL_EXSTYLE, new IntPtr(exStyle & ~WS_EX_LAYERED));
            }
            _layeredByUs.Clear();
        }
    }

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetLayeredWindowAttributes(IntPtr hWnd, out uint crKey, out byte bAlpha, out uint dwFlags);
}
