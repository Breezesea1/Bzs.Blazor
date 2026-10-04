namespace Bzs.Blazor;

internal static class BzsSeverityNames
{
    public static string Name(BzsMessageSeverity severity) => severity switch
    {
        BzsMessageSeverity.Information => "information",
        BzsMessageSeverity.Success => "success",
        BzsMessageSeverity.Warning => "warning",
        BzsMessageSeverity.Error => "error",
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "The message severity is not supported."),
    };

    public static string Name(BzsToastSeverity severity) => severity switch
    {
        BzsToastSeverity.Information => "information",
        BzsToastSeverity.Success => "success",
        BzsToastSeverity.Warning => "warning",
        BzsToastSeverity.Error => "error",
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "The toast severity is not supported."),
    };

    public static BzsIconData Icon(BzsMessageSeverity severity) => severity switch
    {
        BzsMessageSeverity.Information => BzsIcons.Info,
        BzsMessageSeverity.Success => BzsIcons.Success,
        BzsMessageSeverity.Warning => BzsIcons.Warning,
        BzsMessageSeverity.Error => BzsIcons.Error,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "The message severity is not supported."),
    };

    public static BzsIconData Icon(BzsToastSeverity severity) => severity switch
    {
        BzsToastSeverity.Information => BzsIcons.Info,
        BzsToastSeverity.Success => BzsIcons.Success,
        BzsToastSeverity.Warning => BzsIcons.Warning,
        BzsToastSeverity.Error => BzsIcons.Error,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "The toast severity is not supported."),
    };

    public static bool IsAssertive(BzsMessageSeverity severity) => severity == BzsMessageSeverity.Error;

    public static bool IsAssertive(BzsToastSeverity severity) => severity == BzsToastSeverity.Error;
}
