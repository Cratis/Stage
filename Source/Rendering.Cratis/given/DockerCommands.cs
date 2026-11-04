// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics;

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// Runs docker as a process, with separate streams and a bounded time.
/// </summary>
/// <remarks>
/// It never enters <see cref="GeneratedApplicationBuilds"/>, so cleanup cannot wait behind a generated build.
/// </remarks>
public sealed class DockerCommands : IDockerCommands
{
    /// <inheritdoc/>
    public async Task<DockerCommandResult> Run(IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("docker")
        {
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
        catch (Win32Exception exception)
        {
            return new(-1, string.Empty, exception.Message);
        }

        var standardOutput = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var standardError = process.StandardError.ReadToEndAsync(CancellationToken.None);
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        bound.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(bound.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            return new(-1, await standardOutput, $"docker timed out or was cancelled after {timeout.TotalSeconds:F0}s. {await standardError}");
        }

        return new(process.ExitCode, await standardOutput, await standardError);
    }
}
