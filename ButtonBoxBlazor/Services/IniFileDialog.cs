using System.Runtime.InteropServices;

/// <summary>Native "Open file" dialog restricted to *.ini files; it opens on the machine the server runs on.</summary>
public static class IniFileDialog
{
    private const int OFN_HIDEREADONLY = 0x4;
    private const int OFN_NOCHANGEDIR = 0x8;
    private const int OFN_PATHMUSTEXIST = 0x800;
    private const int OFN_FILEMUSTEXIST = 0x1000;
    private const int OFN_EXPLORER = 0x80000;
    private const int MaxPath = 1024;

    /// <summary>Returns the selected *.ini file, or null if the dialog was cancelled or is unavailable.</summary>
    public static Task<string?> PickAsync(string? initialDirectory)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult<string?>(null);
        }

        var completion = new TaskCompletionSource<string?>();
        // Common dialogs need an STA thread.
        var thread = new Thread(() =>
        {
            try
            {
                completion.SetResult(Show(initialDirectory));
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        return completion.Task;
    }

    private static string? Show(string? initialDirectory)
    {
        var buffer = Marshal.AllocHGlobal(MaxPath * sizeof(char));
        try
        {
            Marshal.WriteInt16(buffer, 0);
            var dialog = new OPENFILENAME
            {
                lStructSize = Marshal.SizeOf<OPENFILENAME>(),
                lpstrFilter = "INI files (*.ini)\0*.ini\0\0",
                nFilterIndex = 1,
                lpstrFile = buffer,
                nMaxFile = MaxPath,
                lpstrInitialDir = initialDirectory,
                lpstrTitle = "Select controls.ini",
                lpstrDefExt = "ini",
                Flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_HIDEREADONLY | OFN_NOCHANGEDIR
            };

            if (!GetOpenFileName(ref dialog))
            {
                return null;
            }

            var path = Marshal.PtrToStringUni(buffer);
            // The filter only hides other files; a typed name can still point elsewhere.
            return path is { Length: > 0 } && string.Equals(Path.GetExtension(path), ".ini", StringComparison.OrdinalIgnoreCase) ? path : null;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetOpenFileNameW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetOpenFileName(ref OPENFILENAME ofn);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OPENFILENAME
    {
        public int lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public string? lpstrFilter;
        public string? lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public IntPtr lpstrFile;
        public int nMaxFile;
        public string? lpstrFileTitle;
        public int nMaxFileTitle;
        public string? lpstrInitialDir;
        public string? lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public string? lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public string? lpTemplateName;
        public IntPtr pvReserved;
        public int dwReserved;
        public int FlagsEx;
    }
}
