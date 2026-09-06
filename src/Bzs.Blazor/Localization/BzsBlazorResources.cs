namespace Bzs.Blazor.Localization;

/// <summary>
/// Marks the shared Bzs.Blazor resource set.
/// </summary>
/// <remarks>
/// The neutral resource is English. Culture-specific resource files use the
/// standard .NET suffix, such as <c>.zh-Hans.resx</c>, and fall back to the
/// neutral resource when a key is not translated.
/// </remarks>
/// <remarks>
/// The folder-path namespace is load-bearing: the SDK derives the resx manifest
/// name from it, so the manifest must stay identical to this type's full name.
/// The localization tests and the release script pin the exact name.
/// </remarks>
internal sealed class BzsBlazorResources
{
}
