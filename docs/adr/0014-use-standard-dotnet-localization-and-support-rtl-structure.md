# Use standard .NET localization and support RTL structure

Bzs.Blazor will localize only library-owned text through standard `.resx` resources and `IStringLocalizer`, shipping English and Simplified Chinese initially while following `CultureInfo.CurrentUICulture`. Business content remains consumer-owned, date and number behavior follows the active culture, CSS uses logical properties and inherits the host document direction, and the first release supports correct RTL structure without claiming built-in translations for RTL languages.
