using Bzs.Blazor;
using Bzs.Blazor.Demo.Client;
using Bzs.Blazor.Demo.WebAssembly;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddBzsBlazor();
builder.Services.AddDemoCatalog();

var host = builder.Build();
DemoCulture.ApplyCurrentCulture(new Uri(host.Services.GetRequiredService<NavigationManager>().Uri));

await host.RunAsync();
