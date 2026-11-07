// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// Runs container commands for the generated-application lifecycle.
/// </summary>
public interface IDockerCommands
{
    /// <summary>
    /// Runs one docker command with a bounded time.
    /// </summary>
    /// <param name="arguments">The arguments after docker.</param>
    /// <param name="timeout">How long the command may run.</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>The command outcome. It never throws for a failing or timed-out command.</returns>
    Task<DockerCommandResult> Run(IReadOnlyList<string> arguments, TimeSpan timeout, CancellationToken cancellationToken);
}
