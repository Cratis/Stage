// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;

/// <summary>
/// Runs a build tool in a generated application and keeps its exit code with its output.
/// </summary>
public static class ApplicationProcess
{
    /// <summary>
    /// Runs a tool to completion, or kills it with its whole process tree when it runs past the deadline.
    /// </summary>
    /// <param name="directory">The working directory.</param>
    /// <param name="timeout">How long the tool may run.</param>
    /// <param name="fileName">The tool.</param>
    /// <param name="arguments">The tool's arguments.</param>
    /// <returns>The exit code and the combined output. A tool that could not start or timed out has exit code -1.</returns>
    public static async Task<ProcessResult> Run(string directory, TimeSpan timeout, string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            return new(-1, $"{fileName} could not start: {exception.Message}");
        }

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(deadline.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            return new(-1, $"{fileName} {string.Join(' ', arguments)} did not finish within {timeout}.{Environment.NewLine}{await standardOutput}{await standardError}");
        }

        return new(process.ExitCode, $"{await standardOutput}{await standardError}");
    }
}

/// <summary>
/// The outcome of a tool run.
/// </summary>
/// <param name="ExitCode">The exit code; -1 when the tool could not start or timed out.</param>
/// <param name="Output">The combined standard output and error.</param>
public sealed record ProcessResult(int ExitCode, string Output);
