using Bzs.Blazor;
using Bzs.Blazor.Demo.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBzsBlazor();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

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
