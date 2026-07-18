using System.Globalization;
using Bzs.Blazor;
using Bzs.Blazor.Consumer.Components;
using Microsoft.AspNetCore.StaticFiles;

var culture = CultureInfo.GetCultureInfo("zh-Hans");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBzsBlazor();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();
var contentTypes = new FileExtensionContentTypeProvider();
contentTypes.Mappings[".dat"] = "application/octet-stream";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypes,
});
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Bzs.Blazor.Consumer.Client._Imports).Assembly);

app.Run();
