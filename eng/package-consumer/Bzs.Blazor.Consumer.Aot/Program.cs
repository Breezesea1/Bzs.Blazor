using System.Globalization;
using Bzs.Blazor;
using Bzs.Blazor.Consumer.Aot;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddBzsBlazor();
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var culture = CultureInfo.GetCultureInfo("zh-Hans");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

await builder.Build().RunAsync();
