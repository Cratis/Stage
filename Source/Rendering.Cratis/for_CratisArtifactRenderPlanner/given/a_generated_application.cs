// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Diagnostics;
using System.Text;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class a_generated_application : a_register_project_render_request
{
    protected DirectoryInfo _evidence = null!;
    DirectoryInfo? _application;

    void Establish()
    {
        var invocation = Guid.NewGuid().ToString("N");
        var workspace = Path.Combine(WorktreeRoot(), ".ai-work", "stage-namespace");
        _evidence = Directory.CreateDirectory(Path.Combine(workspace, $"verification-{invocation}"));
        _application = new DirectoryInfo(Path.Combine(workspace, $"generated-{invocation}"));
        try
        {
            _application.Create();
            File.WriteAllText(Path.Combine(_evidence.FullName, "lifecycle.log"), $"Created: {_application.FullName}\nStarted: {DateTimeOffset.UtcNow:O}\n");
            var plan = CratisRendering.Plan(_model, _executionPlan, _request.Scope, new("BackendHost", "Acme.projectAPI"));
            plan.Success.ShouldBeTrue();
            foreach (var artifact in plan.Artifacts)
            {
                var path = Path.Combine(_application.FullName, artifact.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, [.. artifact.Bytes]);
            }
        }
        catch
        {
            Cleanup();
            throw;
        }
    }

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

    protected async Task<string> Run(string logName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = _application!.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
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
        if (timedOut || truncated || process.ExitCode != 0)
        {
            throw new GeneratedApplicationVerificationFailed(command, logPath, output);
        }

        return output;
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
