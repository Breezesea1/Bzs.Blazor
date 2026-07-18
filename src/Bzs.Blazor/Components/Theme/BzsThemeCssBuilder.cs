using System.Text;

namespace Bzs.Blazor;

internal static class BzsThemeCssBuilder
{
    public static string Build(string scopeId, BzsTheme theme)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeId);
        ArgumentNullException.ThrowIfNull(theme);

        var builder = new StringBuilder(2_048);
        AppendScheme(builder, scopeId, "light", theme.LightColors, theme.LightDepth, theme);
        AppendScheme(builder, scopeId, "dark", theme.DarkColors, theme.DarkDepth, theme);
        AppendAccessibilityOverrides(builder, scopeId);
        return builder.ToString();
    }

    private static void AppendScheme(
        StringBuilder builder,
        string scopeId,
        string mode,
        BzsThemeColors colors,
        BzsThemeDepth depth,
        BzsTheme theme)
    {
        builder.Append("[data-bzs-theme-scope=\"")
            .Append(scopeId)
            .Append("\"][data-bzs-theme=\"")
            .Append(mode)
            .Append("\"]{");

        Append(builder, "canvas", colors.Canvas);
        Append(builder, "surface", colors.Surface);
        Append(builder, "surface-raised", colors.SurfaceRaised);
        Append(builder, "surface-inset", colors.SurfaceInset);
        Append(builder, "surface-overlay", colors.SurfaceOverlay);
        Append(builder, "text", colors.Text);
        Append(builder, "text-muted", colors.TextMuted);
        Append(builder, "border", colors.Border);
        Append(builder, "focus-ring", colors.FocusRing);
        Append(builder, "primary", colors.Primary);
        Append(builder, "on-primary", colors.OnPrimary);
        Append(builder, "success", colors.Success);
        Append(builder, "warning", colors.Warning);
        Append(builder, "error", colors.Error);
        Append(builder, "info", colors.Info);
        Append(builder, "disabled-surface", colors.DisabledSurface);
        Append(builder, "disabled-text", colors.DisabledText);
        Append(builder, "shadow-raised", depth.RaisedShadow);
        Append(builder, "shadow-inset", depth.InsetShadow);
        Append(builder, "shadow-overlay", depth.OverlayShadow);
        Append(builder, "shadow-focus", depth.FocusShadow);
        Append(builder, "radius-control", theme.Shape.ControlRadius);
        Append(builder, "radius-container", theme.Shape.ContainerRadius);
        Append(builder, "radius-overlay", theme.Shape.OverlayRadius);
        Append(builder, "border-width", theme.Shape.BorderWidth);
        Append(builder, "font-family", theme.Typography.FontFamily);
        Append(builder, "font-size", theme.Typography.FontSize);
        Append(builder, "font-size-small", theme.Typography.SmallFontSize);
        Append(builder, "line-height", theme.Typography.LineHeight);
        Append(builder, "font-weight-regular", theme.Typography.FontWeightRegular);
        Append(builder, "font-weight-medium", theme.Typography.FontWeightMedium);
        Append(builder, "font-weight-bold", theme.Typography.FontWeightBold);
        Append(builder, "motion-fast", theme.Motion.FastDuration);
        Append(builder, "motion-normal", theme.Motion.NormalDuration);
        Append(builder, "motion-slow", theme.Motion.SlowDuration);
        Append(builder, "motion-easing", theme.Motion.Easing);
        builder.Append('}');
    }

    private static void Append(StringBuilder builder, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Theme token '{name}' cannot be empty.");
        }

        if (value.IndexOfAny([';', '{', '}', '<', '>', '@', '!', '\r', '\n']) >= 0
            || value.Contains("url(", StringComparison.OrdinalIgnoreCase)
            || value.Contains("expression(", StringComparison.OrdinalIgnoreCase)
            || !HasBalancedCssValueSyntax(value))
        {
            throw new InvalidOperationException(
                $"Theme token '{name}' contains CSS syntax that is not allowed in a semantic token value.");
        }

        builder.Append("--bzs-")
            .Append(name)
            .Append(':')
            .Append(value.Trim())
            .Append(';');
    }

    private static bool HasBalancedCssValueSyntax(string value)
    {
        var parentheses = 0;
        var quote = '\0';
        var escaped = false;

        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            var next = index + 1 < value.Length ? value[index + 1] : '\0';
            if (current == '/' && next == '*' || current == '*' && next == '/')
            {
                return false;
            }

            if (quote != '\0')
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (current == '\\')
                {
                    escaped = true;
                }
                else if (current == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            if (current is '\'' or '"')
            {
                quote = current;
            }
            else if (current == '(')
            {
                parentheses++;
            }
            else if (current == ')')
            {
                if (parentheses == 0)
                {
                    return false;
                }

                parentheses--;
            }
        }

        return quote == '\0' && parentheses == 0 && !escaped;
    }

    private static void AppendAccessibilityOverrides(StringBuilder builder, string scopeId)
    {
        var selector = $"[data-bzs-theme-scope=\"{scopeId}\"][data-bzs-theme]";
        builder.Append("@media (prefers-reduced-motion:reduce){")
            .Append(selector)
            .Append("{--bzs-motion-fast:0ms;--bzs-motion-normal:0ms;--bzs-motion-slow:0ms;}}")
            .Append("@media (forced-colors:active){")
            .Append(selector)
            .Append("{--bzs-border:CanvasText;--bzs-focus-ring:Highlight;--bzs-shadow-raised:none;")
            .Append("--bzs-shadow-inset:none;--bzs-shadow-overlay:none;--bzs-shadow-focus:0 0 0 2px Highlight;}}");
    }
}
