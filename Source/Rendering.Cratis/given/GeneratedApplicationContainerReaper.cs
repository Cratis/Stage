// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// Removes containers left behind by killed runs of this fixture family, and nothing else.
/// </summary>
/// <param name="docker">Runs the container commands.</param>
/// <param name="guard">Removes by identifier after proving ownership.</param>
/// <param name="processes">Probes owning processes.</param>
/// <param name="clock">The clock.</param>
/// <param name="current">The owner of the current run, which is never reaped.</param>
public sealed class GeneratedApplicationContainerReaper(IDockerCommands docker, GeneratedApplicationContainerGuard guard, IOwnerProcesses processes, TimeProvider clock, GeneratedApplicationContainerOwner current)
{
    /// <summary>
    /// Lists the labelled containers, and removes those proven stale.
    /// </summary>
    /// <returns>The report. A failure to list removes nothing.</returns>
    public async Task<ContainerReapReport> Reap()
    {
        var listing = await docker.Run(
            ["ps", "--all", "--no-trunc", "--quiet", "--filter", $"label={GeneratedApplicationContainerOwner.FamilyKey}={GeneratedApplicationContainerOwner.FamilyValue}"],
            GeneratedApplicationContainerGuard.CommandTimeout,
            CancellationToken.None);
        if (listing.ExitCode != 0)
        {
            return new(0, [$"listing failed with exit code {listing.ExitCode}; nothing was removed"]);
        }

        var lines = new List<string>();
        var removed = 0;
        foreach (var line in listing.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!GeneratedApplicationContainerId.TryParse(line, out var id))
            {
                lines.Add("ignored: a listed entry was not a full container identifier");
                continue;
            }

            var (wasRemoved, text) = await Examine(id);
            removed += wasRemoved ? 1 : 0;
            lines.Add($"{id}: {text}");
        }

        return new(removed, lines);
    }

    async Task<(bool Removed, string Text)> Examine(GeneratedApplicationContainerId id)
    {
        var inspection = await guard.Inspect(id);
        if (inspection.Kind != GeneratedApplicationContainerInspectionKind.Found)
        {
            return (false, $"kept: inspect was {inspection.Kind}: {inspection.Reason}");
        }

        var read = GeneratedApplicationContainerOwner.TryRead(inspection.Labels, out var owner);
        var decision = GeneratedApplicationContainerStaleness.Decide(read ? owner : null, current, clock.GetUtcNow(), processes);
        if (!decision.Reap)
        {
            return (false, decision.Reason);
        }

        var removal = await guard.Remove(id, owner.RunToken);
        return (removal.Outcome == ContainerRemoval.Removed, removal.Outcome == ContainerRemoval.Removed ? decision.Reason : $"kept: removal was {removal.Outcome}");
    }
}
