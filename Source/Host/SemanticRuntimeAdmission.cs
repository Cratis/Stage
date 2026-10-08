// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host;

internal sealed record SemanticAdmissionEntry(string Artifact, string Kind, string Status, string? Capability, string? Details);

internal sealed class SemanticRuntimeAdmission
{
    internal SemanticRuntimeAdmission(SemanticExecutionPlan plan)
    {
        var entries = new List<SemanticAdmissionEntry>();
        var blocking = new List<SemanticAdmissionEntry>();

        // ESM v5-v7 constructs the runtime cannot execute refuse the whole model rather than running without them:
        // a reaction, capture or trigger would never fire, and a generated value or response would be mis-executed.
        var application = plan.Model.Application;
        foreach (var feature in SemanticVersionFeatures.InApplication(application).Concat(SemanticVersionFeatures.Slices(application).SelectMany(SemanticVersionFeatures.InSlice)))
        {
            var entry = Refused(feature);
            entries.Add(entry);
            blocking.Add(entry);
        }
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
            if (SemanticVersionFeatures.InCommand(command).FirstOrDefault() is { } feature)
            {
                var entry = Refused(feature);
                entries.Add(entry);
                blocking.Add(entry);
                continue;
            }

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
            // Live specifications never run, so a v5-v7 construct in one is reported precisely without blocking the model.
            entries.Add(SemanticVersionFeatures.InSpecification(specification).FirstOrDefault() is { } feature
                ? Refused(feature)
                : new(specification.Id.ToString(), "specification", "unsupported", "Specification", "Live specification execution is not available; use the specification runner."));
        }

        Entries = entries;
        Blocking = [.. entries.Where(entry => entry.Kind == "event" && entry.Status == "unsupported"), .. blocking];
    }

    internal IReadOnlyList<SemanticAdmissionEntry> Entries { get; }

    internal IReadOnlyList<SemanticAdmissionEntry> Blocking { get; }

    static SemanticAdmissionEntry Refused(SemanticVersionFeature feature) =>
        new(feature.Artifact.ToString(), feature.Kind, "unsupported", feature.Capability.ToString(), $"{feature.Code}: {feature.Message}");
}
