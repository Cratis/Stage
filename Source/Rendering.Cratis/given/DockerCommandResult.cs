// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// The outcome of one container command, with the output streams kept apart.
/// </summary>
/// <param name="ExitCode">The process exit code, or a non-zero value when the command could not run or timed out.</param>
/// <param name="StandardOutput">What the command wrote to standard output.</param>
/// <param name="StandardError">What the command wrote to standard error.</param>
public sealed record DockerCommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    /// <summary>
    /// Gets both streams, for evidence text.
    /// </summary>
    public string Output => $"{StandardOutput}{StandardError}";
}
