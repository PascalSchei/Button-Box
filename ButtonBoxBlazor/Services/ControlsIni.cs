using System.Globalization;

/// <summary>Reads Condor's controls.ini and locates it in the usual per-pilot folders.</summary>
public static class ControlsIni
{
    /// <summary>DirectInput GUID of the system keyboard (GUID_SysKeyboard); the section holds one scan code per function.</summary>
    public const string KeyboardSection = "{6F1D2B61-D5A0-11CF-BFC7-444553540000}";

    /// <summary>Returns function number to DirectInput keyboard value; unassigned functions (value 0) and malformed lines are skipped.</summary>
    public static IReadOnlyDictionary<int, int> ParseKeyboard(string text)
    {
        var keys = new Dictionary<int, int>();
        var inKeyboard = false;

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith('['))
            {
                inKeyboard = line.Equals($"[{KeyboardSection}]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inKeyboard)
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator > 0
                && int.TryParse(line.AsSpan(0, separator), NumberStyles.None, CultureInfo.InvariantCulture, out var function)
                && int.TryParse(line.AsSpan(separator + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var value)
                && value is > 0 and <= 255)
            {
                keys[function] = value;
            }
        }

        return keys;
    }

    /// <summary>Folders that can contain "Condor3\Pilots" (the redirected Documents folder and the OneDrive variants).</summary>
    public static IEnumerable<string> DefaultDocumentFolders()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new List<string> { Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) };

        if (profile.Length > 0)
        {
            candidates.Add(Path.Combine(profile, "Documents"));
        }

        foreach (var variable in new[] { "OneDrive", "OneDriveConsumer", "OneDriveCommercial" })
        {
            if (Environment.GetEnvironmentVariable(variable) is { Length: > 0 } oneDrive)
            {
                candidates.Add(Path.Combine(oneDrive, "Documents"));
            }
        }

        return candidates.Where(c => c.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Finds "&lt;documents&gt;\Condor3\Pilots\&lt;pilot&gt;\controls.ini" files, most recently modified first.</summary>
    public static IReadOnlyList<string> FindControlsFiles(IEnumerable<string> documentFolders)
    {
        var found = new List<string>();

        foreach (var documents in documentFolders)
        {
            var pilots = Path.Combine(documents, "Condor3", "Pilots");
            if (!Directory.Exists(pilots))
            {
                continue;
            }

            try
            {
                foreach (var pilot in Directory.EnumerateDirectories(pilots))
                {
                    var file = Path.Combine(pilot, "controls.ini");
                    if (File.Exists(file))
                    {
                        found.Add(file);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // An unreadable folder is skipped; the remaining locations are still searched.
            }
        }

        return [.. found.Distinct(StringComparer.OrdinalIgnoreCase).OrderByDescending(File.GetLastWriteTimeUtc)];
    }
}
