using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;

namespace Bzs.Blazor.BrowserTests;

public sealed class StandaloneWebAssemblyFixture : IAsyncLifetime
{
    private const string StandaloneBasePath = "/Bzs.Blazor";
    private WebApplication? _application;
    private string? _temporaryDirectory;

    public string BaseUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        try
        {
            _temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                $"Bzs.Blazor.BrowserTests-{Guid.NewGuid():N}");
            var publishDirectory = Path.Combine(_temporaryDirectory, "publish");
            Directory.CreateDirectory(publishDirectory);

            var repositoryRoot = FindRepositoryRoot();
            var projectPath = Path.Combine(
                repositoryRoot,
                "samples",
                "Bzs.Blazor.Demo",
                "Bzs.Blazor.Demo.WebAssembly",
                "Bzs.Blazor.Demo.WebAssembly.csproj");

            await PublishAsync(repositoryRoot, projectPath, publishDirectory);

            var webRoot = Path.Combine(publishDirectory, "wwwroot");
            if (!File.Exists(Path.Combine(webRoot, "index.html")))
            {
                throw new InvalidOperationException(
                    $"The standalone WebAssembly publish did not produce {Path.Combine(webRoot, "index.html")}.");
            }

            await SetPublishedBasePathAsync(webRoot);

            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            _application = builder.Build();

            var staticFiles = new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(webRoot),
                ServeUnknownFileTypes = true,
                DefaultContentType = "application/octet-stream",
            };
            _application.UsePathBase(StandaloneBasePath);
            _application.UseStaticFiles(staticFiles);
            _application.MapFallbackToFile("index.html", staticFiles);

            await _application.StartAsync();
            BaseUrl = _application.Services
                .GetRequiredService<IServer>()
                .Features
                .Get<IServerAddressesFeature>()!
                .Addresses
                .Single()
                .TrimEnd('/')
                + StandaloneBasePath;
        }
        catch
        {
            try
            {
                await DisposeAsync();
            }
            catch
            {
                // Preserve the fixture initialization failure when cleanup also fails.
            }

            throw;
        }
    }

    public async Task DisposeAsync()
    {
        Exception? cleanupException = null;
        var application = _application;
        _application = null;
        if (application is not null)
        {
            try
            {
                await application.StopAsync();
            }
            catch (Exception exception)
            {
                cleanupException = exception;
            }

            try
            {
                await application.DisposeAsync();
            }
            catch (Exception exception)
            {
                cleanupException ??= exception;
            }
        }

        var temporaryDirectory = _temporaryDirectory;
        _temporaryDirectory = null;
        if (temporaryDirectory is not null && Directory.Exists(temporaryDirectory))
        {
            try
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
            catch (Exception exception)
            {
                cleanupException ??= exception;
            }
        }

        if (cleanupException is not null)
        {
            ExceptionDispatchInfo.Capture(cleanupException).Throw();
        }
    }

    private static async Task PublishAsync(
        string repositoryRoot,
        string projectPath,
        string publishDirectory)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        startInfo.ArgumentList.Add("publish");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--output");
        startInfo.ArgumentList.Add(publishDirectory);
        startInfo.ArgumentList.Add("--no-restore");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The standalone WebAssembly publish could not be started.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        var exitTask = process.WaitForExitAsync();
        var completed = await Task.WhenAny(exitTask, Task.Delay(TimeSpan.FromSeconds(90)));
        if (completed != exitTask)
        {
            Exception? terminationException = null;
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception exception)
            {
                terminationException = exception;
            }

            var terminated = await Task.WhenAny(
                exitTask,
                Task.Delay(TimeSpan.FromSeconds(10)));
            if (terminated != exitTask)
            {
                throw new TimeoutException(
                    "The standalone WebAssembly publish did not exit within 90 seconds "
                    + "and could not be terminated within 10 seconds.",
                    terminationException);
            }

            await exitTask;
            await Task.WhenAll(standardOutput, standardError);
            throw new TimeoutException(
                "The standalone WebAssembly publish did not exit within 90 seconds.");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"The standalone WebAssembly publish exited with code {process.ExitCode}.{Environment.NewLine}"
                + $"stdout:{Environment.NewLine}{await standardOutput}{Environment.NewLine}"
                + $"stderr:{Environment.NewLine}{await standardError}");
        }

        await Task.WhenAll(standardOutput, standardError);
    }

    private static async Task SetPublishedBasePathAsync(string webRoot)
    {
        var indexPath = Path.Combine(webRoot, "index.html");
        var index = await File.ReadAllTextAsync(indexPath);
        var updatedIndex = index.Replace(
            "<base href=\"/\" />",
            $"<base href=\"{StandaloneBasePath}/\" />",
            StringComparison.Ordinal);
        if (string.Equals(index, updatedIndex, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The standalone WebAssembly index did not contain the expected root base element.");
        }

        await File.WriteAllTextAsync(indexPath, updatedIndex);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Bzs.Blazor.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Bzs.Blazor repository root.");
    }
}

[CollectionDefinition(Name)]
public sealed class StandaloneWebAssemblyCollection : ICollectionFixture<StandaloneWebAssemblyFixture>
{
    public const string Name = "Bzs.Blazor Standalone WebAssembly";
}
