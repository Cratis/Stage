// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_verifying_a_standalone_generated_application : a_register_project_render_request
{
    DirectoryInfo _application = null!;
    ProcessResult _debugBuild = null!;
    ProcessResult _debugTest = null!;
    ProcessResult _releaseBuild = null!;
    bool _dockerAvailable;
    string _dockerEvidence = null!;
    string _healthEvidence = null!;

    void Establish()
    {
        _application = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"stage-generated-application-{Guid.NewGuid():N}"));
        var plan = CratisRendering.Plan(_model, _executionPlan, new(ArtifactRenderScopeKind.Application, _model.Application.Id), _options);
        if (!plan.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, plan.Diagnostics.Select(_ => $"{_.Code}: {_.Message}")));
        }

        foreach (var artifact in plan.Artifacts)
        {
            var path = Path.Combine(_application.FullName, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, [.. artifact.Bytes]);
        }
    }

    async Task Because()
    {
        _debugBuild = await Run("dotnet", "build", "Projects.csproj", "-c", "Debug", "--nologo");
        _debugTest = _debugBuild.ExitCode == 0
            ? await Run("dotnet", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--nologo")
            : new(-1, "Debug tests were not run because the Debug build failed.");
        _releaseBuild = await Run("dotnet", "build", "Projects.csproj", "-c", "Release", "--nologo");

        var docker = await Run("docker", "version", "--format", "{{.Server.Version}}");
        _dockerAvailable = docker.ExitCode == 0;
        _dockerEvidence = _dockerAvailable
            ? $"Docker available: {docker.Output.Trim()}"
            : $"Docker unavailable: {docker.Output.Trim()}";
        if (_dockerAvailable && _releaseBuild.ExitCode == 0)
        {
            _healthEvidence = await VerifyHealth();
        }
        else
        {
            _healthEvidence = _dockerAvailable
                ? "Health verification was blocked because the Release build failed."
                : _dockerEvidence;
        }
    }

    async Task Destroy()
    {
        if (_dockerAvailable)
        {
            await Run("docker", "rm", "--force", "--volumes", DockerContainerName());
        }

        if (_application.Exists)
        {
            _application.Delete(recursive: true);
        }
    }

    [Fact] void should_build_the_debug_host_and_inline_generated_specifications_without_warnings() => FailureOutput(_debugBuild).ShouldEqual(string.Empty);
    [Fact] void should_run_the_inline_generated_specifications() => _debugTest.ExitCode.ShouldEqual(0);
    [Fact] void should_build_the_release_host_without_warnings() => FailureOutput(_releaseBuild).ShouldEqual(string.Empty);
    [Fact] void should_record_docker_availability_explicitly() => _dockerEvidence.ShouldNotBeEmpty();
    [Fact] void should_pass_health_when_docker_is_available() => (_dockerAvailable ? _healthEvidence : _dockerEvidence).ShouldEqual(_dockerAvailable ? "GET /healthz returned 200." : _dockerEvidence);

    async Task<string> VerifyHealth()
    {
        var containerName = DockerContainerName();
        var image = $"cratis/chronicle:{CratisRendering.Dependencies.ChronicleImageVersion}-development";
        var start = await Run(
            "docker",
            "run",
            "--detach",
            "--name",
            containerName,
            "--publish",
            "127.0.0.1::27017",
            "--publish",
            "127.0.0.1::35000",
            image);
        if (start.ExitCode != 0)
        {
            await Run("docker", "rm", "--force", "--volumes", containerName);
            return $"Docker is available, but the pinned Chronicle image failed to pull or start: {start.Output}";
        }

        var evidence = string.Empty;
        ProcessResult removal;
        try
        {
            evidence = await VerifyChronicleContainer(containerName);
        }
        finally
        {
            removal = await Run("docker", "rm", "--force", "--volumes", containerName);
        }

        return removal.ExitCode == 0
            ? evidence
            : $"The pinned Chronicle container could not be removed: {removal.Output}";
    }

    async Task<string> VerifyChronicleContainer(string containerName)
    {
        var mongoDBPort = await MappedPort(containerName, "27017/tcp");
        if (mongoDBPort.Port == 0)
        {
            return mongoDBPort.Failure;
        }

        var chroniclePort = await MappedPort(containerName, "35000/tcp");
        if (chroniclePort.Port == 0)
        {
            return chroniclePort.Failure;
        }

        if (!await WaitForInfrastructure(mongoDBPort.Port, chroniclePort.Port))
        {
            var logs = await Run("docker", "logs", containerName);
            return $"The pinned Chronicle container did not expose ready MongoDB and Chronicle endpoints. {logs.Output}";
        }

        return await VerifyGeneratedHost(mongoDBPort.Port, chroniclePort.Port);
    }

    async Task<string> VerifyGeneratedHost(int mongoDBPort, int chroniclePort)
    {
        var port = AvailablePort();
        var startInfo = CreateStartInfo(
            "dotnet",
            "run",
            "--project",
            "Projects.csproj",
            "-c",
            "Release",
            "--no-build",
            "--urls",
            $"http://127.0.0.1:{port}");
        startInfo.Environment["Cratis__Chronicle__ConnectionString"] = $"chronicle://chronicle-dev-client:chronicle-dev-secret@127.0.0.1:{chroniclePort}";
        startInfo.Environment["Cratis__MongoDB__Server"] = $"mongodb://127.0.0.1:{mongoDBPort}";
        using var host = new Process { StartInfo = startInfo };
        host.Start();
        var standardOutput = host.StandardOutput.ReadToEndAsync();
        var standardError = host.StandardError.ReadToEndAsync();

        try
        {
            using var client = new HttpClient();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            while (!timeout.IsCancellationRequested && !host.HasExited)
            {
                try
                {
                    using var response = await client.GetAsync(new Uri($"http://127.0.0.1:{port}/healthz"), timeout.Token);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return "GET /healthz returned 200.";
                    }
                }
                catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
                {
                    // Infrastructure readiness backoff: the generated host may still be starting.
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500), timeout.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }

            if (!host.HasExited)
            {
                host.Kill(entireProcessTree: true);
                await host.WaitForExitAsync();
            }

            return $"The generated host did not become healthy. {await standardOutput}{await standardError}";
        }
        finally
        {
            if (!host.HasExited)
            {
                host.Kill(entireProcessTree: true);
                await host.WaitForExitAsync();
            }
        }
    }

    async Task<(int Port, string Failure)> MappedPort(string containerName, string containerPort)
    {
        var result = await Run("docker", "port", containerName, containerPort);
        if (result.ExitCode != 0)
        {
            return (0, $"Docker could not query the mapped host port for container port {containerPort}: {result.Output}");
        }

        var endpoints = result.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var separator = endpoints.Length == 1 ? endpoints[0].LastIndexOf(':') : -1;
        if (separator < 0 || !int.TryParse(endpoints[0][(separator + 1)..], out var port))
        {
            return (0, $"Docker returned an invalid mapped host port for container port {containerPort}: {result.Output}");
        }

        return (port, string.Empty);
    }

    static async Task<bool> WaitForInfrastructure(int mongoDBPort, int chroniclePort)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        while (!timeout.IsCancellationRequested)
        {
            if (await AcceptsConnections(mongoDBPort, timeout.Token) &&
                await AcceptsConnections(chroniclePort, timeout.Token))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), timeout.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        return false;
    }

    static async Task<bool> AcceptsConnections(int port, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is SocketException or OperationCanceledException)
        {
            return false;
        }
    }

    async Task<ProcessResult> Run(string fileName, params string[] arguments)
    {
        var startInfo = CreateStartInfo(fileName, arguments);
        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            return new(-1, exception.Message);
        }

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(8));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }

        return new(process.ExitCode, $"{await standardOutput}{await standardError}");
    }

    ProcessStartInfo CreateStartInfo(string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = _application.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    string DockerContainerName() => $"stage-generated-{_application.Name[^12..]}";

    static string FailureOutput(ProcessResult result) =>
        result.ExitCode == 0 &&
        result.Output.Contains("0 Warning(s)", StringComparison.Ordinal) &&
        !result.Output.Contains(": warning ", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : result.Output;

    static int AvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    sealed record ProcessResult(int ExitCode, string Output);
}
