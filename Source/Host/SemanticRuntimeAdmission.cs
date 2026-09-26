// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host;

internal sealed record SemanticAdmissionEntry(string Artifact, string Kind, string Status, string? Capability, string? Details);

internal sealed class SemanticRuntimeAdmission
{
    internal SemanticRuntimeAdmission(SemanticExecutionPlan plan)
    {
        var entries = new List<SemanticAdmissionEntry>();
        var duplicateNames = plan.Events.Values.GroupBy(@event => @event.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var @event in plan.Events.Values)
        {
            string? problem = null;
            if (@event.Revision != EventContractRevision.Initial)
            {
                problem = "Only the initial event revision can be registered in Chronicle.";
            }
            else if (duplicateNames.Contains(@event.Name))
            {
                problem = "Event name is shared by multiple contracts.";
            }
            else if (@event.Name.Contains('+', StringComparison.Ordinal))
            {
                problem = "Event name contains Chronicle's reserved event-generation separator.";
            }
            entries.Add(new(@event.Id.ToString(), "event", problem is null ? "supported" : "unsupported", problem is null ? null : "EventContract", problem));
        }

        foreach (var command in plan.Commands.Values)
        {
            var problem = command.Produces.Any(produced => produced.Destination is null && command.Destination?.Value is null)
                ? "A produced fact requires an allocated destination."
                : null;
            entries.Add(new(
                command.Id.ToString(),
                "command",
                problem is null ? "supported" : "unsupported",
                problem is null ? null : nameof(SemanticExecutionCapability.IdentityAllocation),
                problem));
        }

        foreach (var readModel in plan.ReadModels.Values)
        {
            entries.Add(new(readModel.Id.ToString(), "readModel", "supported", null, null));
        }

        foreach (var query in plan.Queries.Values)
        {
            entries.Add(new(query.Id.ToString(), "query", "supported", null, null));
        }

        foreach (var specification in plan.Specifications.Values)
        {
            entries.Add(new(specification.Id.ToString(), "specification", "unsupported", "Specification", "Live specification execution is not available; use the specification runner."));
        }

        Entries = entries;
    }

    internal IReadOnlyList<SemanticAdmissionEntry> Entries { get; }

    internal IReadOnlyList<SemanticAdmissionEntry> Blocking => [.. Entries.Where(entry => entry.Kind == "event" && entry.Status == "unsupported")];
}
