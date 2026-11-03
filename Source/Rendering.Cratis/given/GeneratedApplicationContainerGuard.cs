// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// The only place a container is removed: by full identifier, after a fresh inspect proves who owns it.
/// </summary>
/// <param name="docker">Runs the container commands.</param>
public sealed class GeneratedApplicationContainerGuard(IDockerCommands docker)
{
    /// <summary>
    /// How long one non-create container command may run.
    /// </summary>
    public static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Inspects a container by its full identifier.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The inspection, confirmed to be for that identifier.</returns>
    public async Task<GeneratedApplicationContainerInspection> Inspect(GeneratedApplicationContainerId id) =>
        GeneratedApplicationContainerInspection.Parse(await docker.Run(GeneratedApplicationContainerInspection.Arguments(id), CommandTimeout, CancellationToken.None), id);

    /// <summary>
    /// Checks that a container exists and carries the run token.
    /// </summary>
    /// <param name="id">The full identifier.</param>
    /// <param name="runToken">The run token the container must carry.</param>
    /// <returns>Empty when confirmed, otherwise why it could not be.</returns>
    public async Task<string> Unconfirmed(GeneratedApplicationContainerId id, string runToken)
    {
        var inspection = await Inspect(id);
        if (inspection.Kind != GeneratedApplicationContainerInspectionKind.Found)
        {
            return inspection.Reason;
        }

        return GeneratedApplicationContainerOwner.TryRead(inspection.Labels, out var owner) && owner.RunToken == runToken
            ? string.Empty
            : "the labels do not match this run";
    }

    /// <summary>
    /// Removes a container only when a fresh inspect confirms the identifier and the run token.
    /// </summary>
    /// <param name="id">The full identifier.</param>
    /// <param name="runToken">The run token the container must carry.</param>
    /// <returns>What happened. Anything unproven removes nothing.</returns>
    public async Task<ContainerRemovalResult> Remove(GeneratedApplicationContainerId id, string runToken)
    {
        var inspection = await Inspect(id);
        switch (inspection.Kind)
        {
            case GeneratedApplicationContainerInspectionKind.Missing:
                return new(ContainerRemoval.AlreadyGone, string.Empty);
            case GeneratedApplicationContainerInspectionKind.Unknown:
                return new(ContainerRemoval.Unknown, inspection.Reason);
        }

        if (!GeneratedApplicationContainerOwner.TryRead(inspection.Labels, out var owner) || owner.RunToken != runToken)
        {
            return new(ContainerRemoval.Refused, "the container is not owned by this run");
        }

        var removal = await docker.Run(["rm", "--force", "--volumes", id.Value], CommandTimeout, CancellationToken.None);
        return removal.ExitCode == 0
            ? new(ContainerRemoval.Removed, string.Empty)
            : new(ContainerRemoval.Failed, removal.Output);
    }
}
