using System.Diagnostics;
using System.Net;

namespace Bzs.Blazor.BrowserTests;

public sealed class DemoServerFixture : IAsyncLifetime
{
    private Process? _process;

    public string BaseUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var configuredUrl = Environment.GetEnvironmentVariable("BZS_DEMO_BASE_URL");
        if (!string.IsNullOrWhiteSpace(configuredUrl))
        {
            BaseUrl = configuredUrl.TrimEnd('/');
            return;
        }

        var repositoryRoot = FindRepositoryRoot();
        var projectPath = Path.Combine(
            repositoryRoot,
            "samples",
            "Bzs.Blazor.Demo",
            "Bzs.Blazor.Demo",
            "Bzs.Blazor.Demo.csproj");
        var listeningUrl = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--no-launch-profile");
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add("http://127.0.0.1:0");
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["DetailedErrors"] = "true";

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The Bzs.Blazor Demo process could not be started.");
        _process.OutputDataReceived += (_, args) =>
        {
            const string marker = "Now listening on: ";
            var markerIndex = args.Data?.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex is >= 0)
            {
                listeningUrl.TrySetResult(args.Data![(markerIndex.Value + marker.Length)..].TrimEnd('/'));
            }
        };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        var completed = await Task.WhenAny(
            listeningUrl.Task,
            _process.WaitForExitAsync(),
            Task.Delay(TimeSpan.FromSeconds(90)));
        if (completed != listeningUrl.Task)
        {
            var detail = _process.HasExited
                ? $"exited with code {_process.ExitCode}"
                : "did not report a listening address within 90 seconds";
            throw new InvalidOperationException($"The Bzs.Blazor Demo {detail}.");
        }

        BaseUrl = await listeningUrl.Task;

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTimeOffset.UtcNow.AddSeconds(90);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException(
                    $"The Bzs.Blazor Demo exited before becoming ready with code {_process.ExitCode}.");
            }

            try
            {
                using var response = await client.GetAsync(BaseUrl);
                if (response.StatusCode is HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"The Bzs.Blazor Demo did not become ready at {BaseUrl}.");
    }

    public Task DisposeAsync()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(5_000);
        }

        _process?.Dispose();
        return Task.CompletedTask;
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
public sealed class DemoCollection : ICollectionFixture<DemoServerFixture>
{
    public const string Name = "Bzs.Blazor Demo";
}
