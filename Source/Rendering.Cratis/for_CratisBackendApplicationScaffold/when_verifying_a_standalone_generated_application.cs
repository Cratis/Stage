// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Specs;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_verifying_a_standalone_generated_application(when_verifying_a_standalone_generated_application.context fixture) : IClassFixture<when_verifying_a_standalone_generated_application.context>
{
    [Fact] void should_build_the_debug_application_with_its_specifications_without_warnings() => context.FailureOutput(fixture.DebugBuild).ShouldEqual(string.Empty);
    [Fact] void should_run_the_generated_specifications_from_the_application_project() => fixture.DebugTest.ExitCode.ShouldEqual(0);
    [Fact] void should_execute_a_nonempty_set_of_generated_specifications() => fixture.ExecutedSpecifications().ShouldBeGreaterThan(0);
    [Fact] void should_pass_every_generated_specification() => fixture.PassedSpecifications("generated.trx").ShouldEqual(fixture.ExecutedSpecifications());
    [Fact] void should_compile_every_generated_specification_in_debug() => fixture.CompileSources(fixture.SpecCompile).Intersect(fixture.SpecSources).ShouldContainOnly(fixture.SpecSources);
    [Fact] void should_compile_no_generated_specifications_in_release() => fixture.CompileSources(fixture.WebCompile).Intersect(fixture.SpecSources).ShouldBeEmpty();
    [Fact] void should_cover_a_nonempty_set_of_emitted_specification_files() => fixture.SpecSources.ShouldNotBeEmpty();
    [Fact] void should_build_the_release_application_without_warnings() => context.FailureOutput(fixture.ReleaseBuild).ShouldEqual(string.Empty);
    [Fact] void should_reference_no_test_packages_in_release() => fixture.ReleaseAssets.Contains("\"xunit/", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_accept_the_long_match_and_reject_the_near_miss() => fixture.AssertLongMatchPassed("generated.trx");
    [Fact] void should_record_docker_availability_explicitly() => fixture.DockerEvidence.ShouldNotBeEmpty();
    [Fact] void should_pass_health_when_docker_is_available() => (fixture.DockerAvailable ? fixture.HealthEvidence : fixture.DockerEvidence).ShouldEqual(fixture.DockerAvailable ? "GET /healthz returned 200." : fixture.DockerEvidence);
    [Fact] void should_serve_a_read_model_query_immediately_after_the_first_healthy_response_when_docker_is_available() => (fixture.DockerAvailable ? fixture.FirstQueryStatus : HttpStatusCode.OK).ShouldEqual(HttpStatusCode.OK);

    internal static ExecutableSemanticModel CreateLongMatchModel() => context.CreateLongMatchModel();

    /// <summary>
    /// Renders, builds, tests and serves the generated application once for every fact in the class.
    /// </summary>
    public class context : a_register_project_render_request
    {
        readonly List<string> _timings = [];
        DirectoryInfo _application = null!;
        internal ProcessResult DebugBuild { get; private set; } = null!;
        internal ProcessResult DebugTest { get; private set; } = null!;
        internal ProcessResult ReleaseBuild { get; private set; } = null!;
        internal string ReleaseAssets { get; private set; } = null!;
        internal ProcessResult SpecCompile { get; private set; } = null!;
        internal ProcessResult WebCompile { get; private set; } = null!;
        internal string[] SpecSources { get; private set; } = null!;
        internal bool DockerAvailable { get; private set; }
        internal string DockerEvidence { get; private set; } = null!;
        internal string HealthEvidence { get; private set; } = null!;
        internal HttpStatusCode FirstQueryStatus { get; private set; }

        void Establish()
        {
            var rendering = Stopwatch.StartNew();
            _model = CreateLongMatchModel();
            _executionPlan = SemanticExecutionPlan.Compile(_model).Plan!;
            _application = Directory.CreateDirectory(SpecTemporaryRoot.NewPath("stage-generated-application"));
            var plan = CratisRendering.Plan(_model, _executionPlan, new(ArtifactRenderScopeKind.Application, _model.Application.Id), _options);
            if (!plan.Success)
            {
                throw new InvalidOperationException(string.Join(Environment.NewLine, plan.Diagnostics.Select(_ => $"{_.Code}: {_.Message}")));
            }

            SpecSources = [.. plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) &&
            Path.GetFileName(artifact.RelativePath).StartsWith("when_", StringComparison.Ordinal)).Select(artifact => artifact.RelativePath)];
            foreach (var artifact in plan.Artifacts)
            {
                var path = Path.Combine(_application.FullName, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, [.. artifact.Bytes]);
            }

            _timings.Add($"render\t{rendering.Elapsed.TotalSeconds:F1}\t-");
        }

        async Task Because()
        {
            var total = Stopwatch.StartNew();
            try
            {
                await RunPipeline();
            }
            finally
            {
                _timings.Add($"total\t{total.Elapsed.TotalSeconds:F1}\t-");
                WriteTimings();
            }
        }

        async Task RunPipeline()
        {
            DebugBuild = await Timed("debug-build", () => Run("dotnet", "build", "Projects.slnx", "-c", "Debug", "-m:3", "--nologo"));
            DebugTest = DebugBuild.ExitCode == 0
                ? await Timed("debug-test", () => Run("dotnet", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--nologo", "--logger", "trx;LogFileName=generated.trx", "--results-directory", _application.FullName, "-m:3", "--", "RunConfiguration.MaxCpuCount=3", "xUnit.MaxParallelThreads=3"))
                : new(-1, "Debug tests were not run because the Debug build failed.");
            SpecCompile = await Timed("debug-compile-items", () => Run("dotnet", "msbuild", "Projects.csproj", "-p:Configuration=Debug", "-getItem:Compile"));
            WebCompile = await Timed("release-compile-items", () => Run("dotnet", "msbuild", "Projects.csproj", "-p:Configuration=Release", "-getItem:Compile"));
            ReleaseBuild = await Timed("release-build", () => Run("dotnet", "build", "Projects.slnx", "-c", "Release", "-m:3", "--nologo"));
            ReleaseAssets = ReleaseBuild.ExitCode == 0 ? await File.ReadAllTextAsync(Path.Combine(_application.FullName, "obj", "project.assets.json")) : string.Empty;

            var docker = await Timed("docker-version", () => Run("docker", "version", "--format", "{{.Server.Version}}"));
            DockerAvailable = docker.ExitCode == 0;
            DockerEvidence = DockerAvailable
                ? $"Docker available: {docker.Output.Trim()}"
                : $"Docker unavailable: {docker.Output.Trim()}";
            if (DockerAvailable && ReleaseBuild.ExitCode == 0)
            {
                var live = Stopwatch.StartNew();
                HealthEvidence = await VerifyHealth();
                _timings.Add($"live-serve\t{live.Elapsed.TotalSeconds:F1}\t-");
            }
            else
            {
                HealthEvidence = DockerAvailable
                    ? "Health verification was blocked because the Release build failed."
                    : DockerEvidence;
            }
        }

        async Task<T> Timed<T>(string phase, Func<Task<T>> work)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await work();
            var exit = result is ProcessResult process ? process.ExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-";
            _timings.Add($"{phase}\t{stopwatch.Elapsed.TotalSeconds:F1}\t{exit}");
            return result;
        }

        void WriteTimings()
        {
            var directory = Environment.GetEnvironmentVariable("AI_WORK_KEEP");
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllLines(Path.Combine(directory, "standalone-phase-timings.txt"), _timings);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Timings are diagnostics only; an unwritable evidence directory must not fail the verification.
            }
        }

        async Task Destroy()
        {
            try
            {
                if (DockerAvailable)
                {
                    await Run("docker", "rm", "--force", "--volumes", DockerContainerName());
                }
            }
            finally
            {
                SpecTemporaryRoot.Delete(_application.FullName);
            }
        }

        internal static ExecutableSemanticModel CreateLongMatchModel()
        {
            var form = Corpus.SourceForms.Single(form => form.Name == "single");
            var document = form.Documents.Single();
            var source = StandaloneLongMatchSource.AppendTo(document.Text);
            return Compile(form with { Documents = [document with { Bytes = [.. Encoding.UTF8.GetBytes(source)] }] }).Model;
        }

        static void AuthenticateAsProbeCaller(HttpRequestMessage request)
        {
            const string principal = /*lang=json,strict*/ """{"identityProvider":"stage","userId":"standalone-probe","userDetails":"Standalone probe","userRoles":[],"claims":[]}""";
            request.Headers.Add("x-ms-client-principal-id", "standalone-probe");
            request.Headers.Add("x-ms-client-principal-name", "Standalone probe");
            request.Headers.Add("x-ms-client-principal", Convert.ToBase64String(Encoding.UTF8.GetBytes(principal)));
        }

        internal void AssertLongMatchPassed(string fileName)
        {
            var results = System.Xml.Linq.XDocument.Load(Path.Combine(_application.FullName, fileName));
            foreach (var name in new[] { "when_accepting_a_long_match.should_return_the_expected_response", "when_rejecting_a_long_match.should_report_the_expected_first_error" })
            {
                var result = results.Descendants().Single(element => element.Name.LocalName == "UnitTestResult" &&
                    element.Attribute("testName")!.Value.EndsWith($".{name}", StringComparison.Ordinal));
                result.Attribute("outcome")!.Value.ShouldEqual("Passed");
            }
        }

        internal string[] CompileSources(ProcessResult result)
        {
            result.ExitCode.ShouldEqual(0);
            using var document = System.Text.Json.JsonDocument.Parse(result.Output);
            return [.. document.RootElement.GetProperty("Items").GetProperty("Compile").EnumerateArray()
            .Select(item => Path.GetRelativePath(_application.FullName, item.GetProperty("FullPath").GetString()!).Replace('\\', '/'))];
        }

        internal int ExecutedSpecifications(string fileName = "generated.trx") => SpecificationCount(fileName, "executed");

        internal int PassedSpecifications(string fileName) => SpecificationCount(fileName, "passed");

        int SpecificationCount(string fileName, string counter)
        {
            var results = System.Xml.Linq.XDocument.Load(Path.Combine(_application.FullName, fileName));
            var counters = results.Descendants().Single(element => element.Name.LocalName == "Counters");
            return int.Parse(counters.Attribute(counter)!.Value, System.Globalization.CultureInfo.InvariantCulture);
        }

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

            // The query has no authorize clause and renders [Authorize]; the probe authenticates through Arc's forwarded identity headers.
            startInfo.Environment["Cratis__Arc__TrustForwardedIdentityHeaders"] = "true";
            using var host = new Process { StartInfo = startInfo };
            host.Start();
            var standardOutput = host.StandardOutput.ReadToEndAsync();
            var standardError = host.StandardError.ReadToEndAsync();

            try
            {
                using var client = new HttpClient();
                using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                var healthy = false;
                while (!timeout.IsCancellationRequested && !host.HasExited)
                {
                    try
                    {
                        using var response = await client.GetAsync(new Uri($"http://127.0.0.1:{port}/healthz"), timeout.Token);
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            healthy = true;
                            break;
                        }
                    }
                    catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
                    {
                        // Infrastructure readiness backoff: the generated host may still be starting.
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(500), timeout.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                }

                if (healthy)
                {
                    // A single request outside the readiness retry loop must succeed on the first attempt.
                    using var request = new HttpRequestMessage(
                        HttpMethod.Get,
                        new Uri($"http://127.0.0.1:{port}/api/projects/registration/project-lookup/project-by-id?projectId=4fa85f64-5717-4562-b3fc-2c963f66afa7"));
                    AuthenticateAsProbeCaller(request);
                    using var query = await client.SendAsync(request, timeout.Token);
                    FirstQueryStatus = query.StatusCode;
                    return "GET /healthz returned 200.";
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
            // Bounded so a local run does not start every generated application's build at once.
            using var slot = await GeneratedApplicationBuilds.Enter();
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

            // A generated application is a multi-project solution, so MSBuild starts worker nodes. Reused nodes outlive the build and keep the
            // redirected output pipes open, so reading the output would never finish. Build in fresh nodes that exit with the command.
            startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
            startInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
            foreach (var argument in GeneratedApplicationBuilds.Capped(fileName, arguments))
            {
                startInfo.ArgumentList.Add(argument);
            }

            return startInfo;
        }

        string DockerContainerName() => $"stage-generated-{_application.Name[^12..]}";

        internal static string FailureOutput(ProcessResult result) =>
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

        internal sealed record ProcessResult(int ExitCode, string Output);
    }
}
