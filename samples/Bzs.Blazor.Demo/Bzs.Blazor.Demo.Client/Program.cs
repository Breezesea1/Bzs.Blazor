using Bzs.Blazor;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddBzsBlazor();

await builder.Build().RunAsync();
