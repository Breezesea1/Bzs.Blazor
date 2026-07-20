var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Bzs_Blazor_Demo>(
    "bzs-demo",
    launchProfileName: "http");

builder.Build().Run();
