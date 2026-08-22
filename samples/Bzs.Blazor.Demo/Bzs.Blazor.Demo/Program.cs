using Bzs.Blazor;
using Bzs.Blazor.Demo.Client;
using Bzs.Blazor.Demo.Components;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBzsBlazor();
builder.Services.AddDemoCatalog();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    string[] supportedCultures = ["en-US", "zh-Hans"];
    options.SetDefaultCulture("zh-Hans")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
    options.RequestCultureProviders =
    [
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
    ];
});
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (HttpMethods.IsGet(context.Request.Method)
        && context.Request.Headers.Accept.Any(value =>
            value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true))
    {
        if (context.Request.Query.ContainsKey("culture"))
        {
            var requestedCulture = context.Request.Query["culture"].ToString();
            var culture = string.Equals(requestedCulture, "zh-Hans", StringComparison.OrdinalIgnoreCase)
                ? "zh-Hans"
                : string.Equals(requestedCulture, "en-US", StringComparison.OrdinalIgnoreCase)
                    ? "en-US"
                    : null;
            if (culture is null)
            {
                context.Response.Redirect(CreateCultureUrl(context.Request, "zh-Hans"));
                return;
            }

            context.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { IsEssential = true, Path = "/", SameSite = SameSiteMode.Lax });
        }
        else
        {
            var cookieCulture = CookieRequestCultureProvider.ParseCookieValue(
                context.Request.Cookies[CookieRequestCultureProvider.DefaultCookieName] ?? string.Empty)
                ?.Cultures.FirstOrDefault().Value;
            if (string.Equals(cookieCulture, "zh-Hans", StringComparison.OrdinalIgnoreCase)
                || string.Equals(cookieCulture, "en-US", StringComparison.OrdinalIgnoreCase))
            {
                // The URL parameter is the only culture source the WebAssembly bootstrapper reads,
                // so an explicit redirect keeps host and client on the same culture.
                context.Response.Redirect(CreateCultureUrl(context.Request, cookieCulture!));
                return;
            }
        }
    }

    await next();
});
app.UseRequestLocalization();

app.Use(async (context, next) =>
{
    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self' data: blob:; style-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' 'wasm-unsafe-eval' blob:; " +
        "connect-src 'self' ws: wss: blob:; worker-src 'self' blob:; img-src 'self' data:; font-src 'self'";

    if (context.Request.Path.Value?.EndsWith(".styles.css", StringComparison.OrdinalIgnoreCase) == true)
    {
        // WebKit resolves the generated relative preload links against nested routes.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.Remove("Link");
            return Task.CompletedTask;
        });
    }

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bzs.Blazor.Demo.Client._Imports).Assembly);

app.Run();

static string CreateCultureUrl(HttpRequest request, string culture)
{
    var query = QueryString.Create(request.Query.Where(pair =>
        !string.Equals(pair.Key, "culture", StringComparison.OrdinalIgnoreCase)));
    return $"{request.PathBase}{request.Path}{query.Add("culture", culture)}";
}
