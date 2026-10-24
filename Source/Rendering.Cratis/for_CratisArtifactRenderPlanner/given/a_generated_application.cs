// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class a_generated_application : a_register_project_render_request
{
    protected DirectoryInfo _evidence = null!;
    readonly Dictionary<string, string> _artifactHashes = new(StringComparer.Ordinal);
    DirectoryInfo? _application;

    void Establish()
    {
        var invocation = Guid.NewGuid().ToString("N");
        var workspace = Path.Combine(WorktreeRoot(), ".ai-work", "stage-namespace");
        _evidence = Directory.CreateDirectory(Path.Combine(Environment.GetEnvironmentVariable("AI_WORK_KEEP") ?? workspace, $"verification-{invocation}"));
        _application = new DirectoryInfo(Path.Combine(Environment.GetEnvironmentVariable("AI_WORK_OUTPUT") ?? workspace, $"generated-{invocation}"));
        try
        {
            _application.Create();
            File.WriteAllText(Path.Combine(_evidence.FullName, "lifecycle.log"), $"Created: {_application.FullName}\nStarted: {DateTimeOffset.UtcNow:O}\n");
            if (!DeferPlan) RenderApplication(CreatePlan());
        }
        catch
        {
            Cleanup();
            throw;
        }
    }

    protected virtual bool DeferPlan => false;

    protected virtual bool AllowBlockedPlan => false;

    protected void RenderApplication(ArtifactRenderPlan plan)
    {
        if (!AllowBlockedPlan)
        {
            Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Artifact}: {diagnostic.Message}")));
        }
        foreach (var artifact in plan.Artifacts)
        {
            var path = Path.Combine(_application!.FullName, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, [.. artifact.Bytes]);
            _artifactHashes.Add(artifact.RelativePath, artifact.Sha256);
        }

        File.WriteAllText(Path.Combine(_evidence.FullName, "artifact-hashes.json"), JsonSerializer.Serialize(_artifactHashes));
    }

    /// <summary>
    /// Publishes a plan over the rendered application like a removal-aware publisher: it verifies every removal's hash,
    /// backs up each touched file, writes the artifacts, deletes the removals, and restores the backups on any failure.
    /// </summary>
    /// <param name="plan">The plan to publish.</param>
    /// <param name="failAt">An optional path whose write fails, simulating a publisher failure midway.</param>
    /// <exception cref="InvalidOperationException">A removal's file changed since migration evidence was taken.</exception>
    /// <exception cref="IOException">The write of <paramref name="failAt"/> fails.</exception>
    protected void PublishWithRemovals(ArtifactRenderPlan plan, string? failAt = null)
    {
        foreach (var removal in plan.RequiredRemovals)
        {
            var existing = GeneratedPath(removal.RelativePath);
            if (File.Exists(existing) && PlannedArtifact.CreateText(removal.RelativePath, File.ReadAllText(existing)).Sha256 != removal.ExpectedSha256)
            {
                throw new InvalidOperationException($"'{removal.RelativePath}' changed since migration evidence was taken.");
            }
        }

        var touched = plan.Artifacts.Select(artifact => artifact.RelativePath).Concat(plan.RequiredRemovals.Select(removal => removal.RelativePath)).ToArray();
        var backups = touched.ToDictionary(path => path, path => File.Exists(GeneratedPath(path)) ? File.ReadAllBytes(GeneratedPath(path)) : null, StringComparer.Ordinal);
        try
        {
            foreach (var artifact in plan.Artifacts)
            {
                if (artifact.RelativePath == failAt)
                {
                    throw new IOException($"Simulated write failure at '{failAt}'.");
                }

                var path = GeneratedPath(artifact.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, [.. artifact.Bytes]);
            }

            foreach (var removal in plan.RequiredRemovals)
            {
                File.Delete(GeneratedPath(removal.RelativePath));
            }

            // Later runs verify the published bytes, not the ones rendered first, and no longer expect removed files.
            foreach (var artifact in plan.Artifacts)
            {
                _artifactHashes[artifact.RelativePath] = artifact.Sha256;
            }

            foreach (var removal in plan.RequiredRemovals)
            {
                _artifactHashes.Remove(removal.RelativePath);
            }
        }
        catch
        {
            foreach (var (path, bytes) in backups)
            {
                if (bytes is null) File.Delete(GeneratedPath(path));
                else File.WriteAllBytes(GeneratedPath(path), bytes);
            }

            throw;
        }
    }

    protected bool GeneratedFileExists(string relativePath) => File.Exists(GeneratedPath(relativePath));

    string GeneratedPath(string relativePath) => Path.Combine(_application!.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));

    protected virtual ArtifactRenderPlan CreatePlan() =>
        CratisRendering.Plan(_model, _executionPlan, _request.Scope, new("BackendHost", "Acme.projectAPI"));

    void Destroy() => Cleanup();

    protected void Cleanup()
    {
        if (_application is not null && Directory.Exists(_application.FullName))
        {
            // Delete only this fixture's directory, never sibling invocations or their retained evidence.
            Directory.Delete(_application.FullName, recursive: true);
            File.AppendAllText(Path.Combine(_evidence.FullName, "lifecycle.log"), $"Deleted: {_application.FullName}\nFinished: {DateTimeOffset.UtcNow:O}\n");
        }
    }

    protected string ReadGeneratedFile(string relativePath) => File.ReadAllText(Path.Combine(_application!.FullName, relativePath));

    protected string[] GeneratedFiles(string pattern) => [.. Directory.GetFiles(_application!.FullName, pattern, SearchOption.AllDirectories).Select(path => Path.GetRelativePath(_application.FullName, path))];

    protected void AddGeneratedSpecification(string name, string content)
    {
        var project = Directory.GetFiles(_application!.FullName, "*.csproj").Single();
        WriteGeneratedSupport(Path.Combine($"{Path.GetFileNameWithoutExtension(project)}.Specs", name), content);
    }

    // Partial Program hooks must compile into the host, not the separate specification assembly.
    protected void AddGeneratedHostSupport(string name, string content) => WriteGeneratedSupport(Path.Combine("Customizations", name), content);

    void WriteGeneratedSupport(string relativePath, string content)
    {
        var path = Path.Combine(_application!.FullName, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    protected Task<string> Run(string logName, params string[] arguments) => Run(logName, false, arguments);

    // A parity vector with an intentionally wrong expectation must still yield a complete TRX result.
    protected async Task<string> Run(string logName, bool allowFailedTests, params string[] arguments)
    {
        // Bounded so a local run does not start every generated application's build at once.
        using var slot = await GeneratedApplicationBuilds.Enter();
        arguments = GeneratedApplicationBuilds.Capped("dotnet", SpecificationsArguments(arguments));
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = _application!.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        // A generated application is a multi-project solution, so MSBuild starts worker nodes. Reused nodes outlive the build and keep the
        // redirected output pipes open, so reading the output would never finish. Build in fresh nodes that exit with the command.
        startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        startInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var standardOutput = ReadOutput(process.StandardOutput);
        var standardError = ReadOutput(process.StandardError);
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(8));
        var timedOut = false;
        try
        {
            await Task.WhenAll(process.WaitForExitAsync(), standardOutput, standardError).WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = true;
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await Task.WhenAll(process.WaitForExitAsync(), standardOutput, standardError).WaitAsync(TimeSpan.FromSeconds(30));
        }

        var command = $"dotnet {string.Join(' ', arguments)}";
        var stdout = await standardOutput;
        var stderr = await standardError;
        var output = $"{stdout.Text}{stderr.Text}";
        var truncated = stdout.Truncated || stderr.Truncated;
        var logPath = Path.Combine(_evidence.FullName, logName);
        await File.WriteAllTextAsync(logPath, $"{command}\nExit code: {process.ExitCode}\nTimed out: {timedOut}\nTruncated: {truncated}\n{output}");
        foreach (var (relativePath, expectedHash) in _artifactHashes)
        {
            var path = Path.Combine(_application.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var actualHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path))).ToLowerInvariant();
            actualHash.ShouldEqual(expectedHash);
        }

        if (timedOut || truncated || (process.ExitCode != 0 && !(allowFailedTests && process.ExitCode == 1)))
        {
            throw new GeneratedApplicationVerificationFailed(command, logPath, output);
        }

        return output;
    }

    // Callers name the host they rendered; restore/build cover both projects and test targets only specs.
    static string[] SpecificationsArguments(string[] arguments)
    {
        if (arguments.Length < 2 || !arguments[1].EndsWith(".csproj", StringComparison.Ordinal))
        {
            return arguments;
        }

        var project = arguments[1][..^".csproj".Length];
        return arguments[0] switch
        {
            "restore" or "build" => [arguments[0], $"{project}.slnx", .. arguments.Skip(2)],
            "test" => [arguments[0], $"{project}.Specs/{project}.Specs.csproj", .. arguments.Skip(2)],
            _ => arguments
        };
    }

    static async Task<(string Text, bool Truncated)> ReadOutput(StreamReader reader)
    {
        const int limit = 512 * 1024;
        var text = new StringBuilder();
        var buffer = new char[4096];
        var truncated = false;
        int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory())) > 0)
        {
            var retained = Math.Min(count, limit - text.Length);
            text.Append(buffer, 0, retained);
            truncated |= retained < count;
        }

        // Keep draining pipes after the cap to avoid deadlock, but fail rather than hide warnings or errors.
        return (text.ToString(), truncated);
    }

    protected static string BuildWarnings(string output) =>
        output.Contains("0 Warning(s)", StringComparison.Ordinal) &&
        !output.Contains(": warning ", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : output;

    static string WorktreeRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".git")) || Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }
        }

        throw new GeneratedApplicationWorkspaceNotFound();
    }

    sealed class GeneratedApplicationVerificationFailed(string command, string logPath, string output) : Exception(
        $"Generated application verification failed: {command}. Evidence: {logPath}\n{output}");

    sealed class GeneratedApplicationWorkspaceNotFound() : Exception("Generated application specifications require a repository worktree for .ai-work evidence.");
}
#endif
