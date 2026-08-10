using Bzs.Blazor;
using Bzs.Blazor.Demo.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddBzsBlazor();

var host = builder.Build();
DemoCulture.ApplyCurrentCulture(new Uri(host.Services.GetRequiredService<NavigationManager>().Uri));

await host.RunAsync();
