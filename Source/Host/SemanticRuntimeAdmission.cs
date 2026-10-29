// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host;

internal sealed record SemanticAdmissionEntry(string Artifact, string Kind, string Status, string? Capability, string? Details);

internal sealed class SemanticRuntimeAdmission
{
    internal SemanticRuntimeAdmission(SemanticExecutionPlan plan, Func<string, string?>? ruleName = null)
    {
        var entries = new List<SemanticAdmissionEntry>();
        var blocking = new List<SemanticAdmissionEntry>();
        entries.AddRange(RangeDegradations(plan));

        // External integrations remain visible without blocking unrelated local behavior.
        // Other unsupported constructs still refuse the model rather than silently losing behavior.
        foreach (var feature in SemanticRunAdmission.ModelFeatures(plan.Model))
        {
            var details = SemanticRunAdmission.RunsAround(plan.Model.Application, feature);
            var entry = details is null ? Refused(feature) : new(feature.Artifact, feature.Kind, "renderedAround", feature.Capability, details);
            entries.Add(entry);
            if (details is null) blocking.Add(entry);
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
            var (entry, blocksModel) = CommandEntry(plan, command, ruleName);
            entries.Add(entry);
            if (blocksModel) blocking.Add(entry);
        }

        foreach (var reaction in plan.Reactions)
        {
            var refusal = SemanticPlanAdmission.Check(plan, reaction.Id);
            if (refusal is not null)
            {
                entries.Add(new(reaction.Id.ToString(), "reaction", "unsupported", refusal.Capability.ToString(), refusal.Details));
            }
            else if (!entries.Exists(entry => entry.Artifact == reaction.Id.ToString() && entry.Kind == "reaction"))
            {
                entries.Add(new(reaction.Id.ToString(), "reaction", "supported", null, null));
            }
        }

        foreach (var readModel in plan.ReadModels.Values)
        {
            var refusal = SemanticPlanAdmission.Check(plan, readModel.Id);
            entries.Add(new(readModel.Id.ToString(), "readModel", refusal is null ? "supported" : "unsupported", refusal?.Capability.ToString(), refusal?.Details));
        }

        foreach (var query in plan.Queries.Values)
        {
            var refusal = SemanticPlanAdmission.Check(plan, query.Id) ?? SemanticImplementationReach.ForQuery(plan, query);
            entries.Add(new(query.Id.ToString(), "query", refusal is null ? "supported" : "unsupported", refusal?.Capability.ToString(), refusal?.Details));
        }

        foreach (var specification in plan.Specifications.Values)
        {
            // A refused specification does not block the live model; runs report the same typed refusal.
            var refusal = SemanticRunAdmission.Check(plan, specification);
            entries.Add(SemanticRunAdmission.SpecificationFeatures(specification).FirstOrDefault() is { } feature
                ? Refused(feature)
                : new(specification.Id.ToString(), "specification", refusal is null ? "supported" : "unsupported", refusal?.Capability.ToString(), refusal?.Details));
        }

        Entries = entries;
        Blocking = [.. entries.Where(entry => entry.Kind == "event" && entry.Status == "unsupported"), .. blocking];
    }

    internal IReadOnlyList<SemanticAdmissionEntry> Entries { get; }

    internal IReadOnlyList<SemanticAdmissionEntry> Blocking { get; }

    static (SemanticAdmissionEntry Entry, bool BlocksModel) CommandEntry(SemanticExecutionPlan plan, SemanticCommand command, Func<string, string?>? ruleName)
    {
        if (SemanticPlanAdmission.Check(plan, command.Id) is { } planRefusal)
        {
            return (new(command.Id.ToString(), "command", "unsupported", planRefusal.Capability.ToString(), planRefusal.Details), false);
        }
        if (SemanticRunAdmission.RuntimeCommandFeatures(plan, command, ruleName).FirstOrDefault() is { } feature)
        {
            // An implementation body refuses only this command, not the live model.
            return (Refused(feature), !feature.Details.StartsWith($"{SemanticImplementationReach.Code}:", StringComparison.Ordinal));
        }
        if (command.Handler is { } handler)
        {
            return (new(
                command.Id.ToString(),
                "command",
                "unsupported",
                nameof(SemanticExecutionCapability.ImplementationBody),
                $"STAGE-IMPL-002: Command '{command.Name}' is described but not implemented (requirement {handler.RequirementId}); its handler body requires a target."), false);
        }

        return (CommandDestinationEntry(command), false);
    }

    static SemanticAdmissionEntry CommandDestinationEntry(SemanticCommand command)
    {
        var problem = command.Produces.Any(produced => produced.Destination is null && command.Destination?.Value is null)
            ? "A produced fact requires an allocated destination."
            : null;

        return new(
            command.Id.ToString(),
            "command",
            problem is null ? "supported" : "unsupported",
            problem is null ? null : nameof(SemanticExecutionCapability.IdentityAllocation),
            problem);
    }

    static IEnumerable<SemanticAdmissionEntry> RangeDegradations(SemanticExecutionPlan plan)
    {
        if (CratisEsmSupport.Descriptor.Classify(plan.Model.LanguageVersion, plan.Model.SemanticVersion) == EsmVersionStanding.NewerWithinRange)
        {
            yield return new(
                plan.Model.Application.Id.ToString(),
                "application",
                "supported",
                null,
                $"{ArtifactRenderDegradationCodes.NewerEsmWithinDeclaredRange}: ESM pair {plan.Model.LanguageVersion}/{plan.Model.SemanticVersion} is within {CratisEsmSupport.Descriptor.Accepts} but has not been audited by this Stage version.");
        }
    }

    static SemanticAdmissionEntry Refused(SemanticAdmissionFeature feature) =>
        new(feature.Artifact, feature.Kind, "unsupported", feature.Capability, feature.Details);
}
