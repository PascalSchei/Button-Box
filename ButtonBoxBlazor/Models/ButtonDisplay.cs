/// <summary>Which parts of a button are shown on a device. At least one must be active.</summary>
public sealed record ButtonDisplay(bool ShowIcon = true, bool ShowText = true, bool ShowKey = false)
{
    public static readonly ButtonDisplay Default = new();

    public bool IsValid => ShowIcon || ShowText || ShowKey;
}
