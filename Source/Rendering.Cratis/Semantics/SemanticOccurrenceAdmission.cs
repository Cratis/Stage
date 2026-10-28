// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits server-side application triggers without changing the executor or live-host feature inventory.
/// </summary>
internal static partial class SemanticOccurrenceAdmission
{
    internal static IEnumerable<SemanticVersionFeature> ReplacedFeatures(SemanticSlice slice) =>
        SemanticVersionFeatures.NonEventTriggersInSlice(slice).Where(feature =>
            slice.Kind == SemanticSliceKind.Automation && slice.Reactions.Any(reaction => reaction.Id == feature.Artifact &&
                IsDeclarative(reaction)));

    internal static IEnumerable<SemanticVersionFeature> AdmittedTriggers(SemanticApplicationContext context) =>
        SemanticVersionFeatures.TriggersInApplication(context.Application).Where(feature => HasAdmittedConsumer(feature.Artifact, context));

    internal static IEnumerable<SemanticVersionFeature> RefusalsInSlice(SemanticSlice slice, SemanticApplicationContext context)
    {
        if (slice.Kind != SemanticSliceKind.Automation) yield break;
        foreach (var reaction in slice.Reactions.Where(NeedsOccurrenceRefusal))
        {
            if (Reason(reaction, context) is { } reason)
            {
                yield return new(SemanticVersionFeatures.Automation, StageExecutionCapability.Reaction, reaction.Id, "reaction", reason);
            }
        }
    }

    internal static bool IsAdmitted(SemanticReaction reaction, SemanticApplicationContext context) =>
        reaction.Triggers is [{ Kind: SemanticReactionTriggerKind.ApplicationTrigger, RequirementId: null }] && Reason(reaction, context) is null;

    internal static IEnumerable<GeneratedTypeDeclaration> Declarations(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices) =>
        slices.Where(located => located.Slice.Kind == SemanticSliceKind.Automation).SelectMany(located =>
            located.Slice.Reactions.Where(reaction => IsAdmitted(reaction, context)).Select(reaction => Declaration(located, reaction, context)));

    internal static SemanticApplicationTrigger Trigger(SemanticReaction reaction, SemanticApplicationContext context) =>
        context.Application.Triggers.Single(trigger => trigger.Id == reaction.Triggers[0].Source);

    static GeneratedTypeDeclaration Declaration(LocatedSemanticSlice located, SemanticReaction reaction, SemanticApplicationContext context)
    {
        var trigger = Trigger(reaction, context);
        return new(trigger.Id, "Trigger", string.Join('.', located.Path.Select(Identifiers.ToPascalCase)), Identifiers.ToPascalCase(trigger.Name));
    }

    static bool IsDeclarative(SemanticReaction reaction) => reaction.Triggers.All(trigger => trigger.RequirementId is null);

    static bool NeedsOccurrenceRefusal(SemanticReaction reaction) => IsDeclarative(reaction) && reaction.Triggers.Any(trigger => trigger.Kind != SemanticReactionTriggerKind.Event);

    static bool HasAdmittedConsumer(SemanticId trigger, SemanticApplicationContext context) =>
        context.AllSlices.Any(located => located.Slice.Kind == SemanticSliceKind.Automation && located.Slice.Reactions.Any(reaction => IsAdmitted(reaction, context) && reaction.Triggers[0].Source == trigger));

    static int HandlerCount(SemanticApplicationTrigger source, SemanticApplicationContext context) =>
        context.AllSlices.SelectMany(located => located.Slice.Reactions).Count(candidate => candidate.Triggers.Any(item => item.Kind == SemanticReactionTriggerKind.ApplicationTrigger && item.Source == source.Id));

    static string? Reason(SemanticReaction reaction, SemanticApplicationContext context)
    {
        var trigger = reaction.Triggers.FirstOrDefault(trigger => trigger.Kind != SemanticReactionTriggerKind.Event);
        if (trigger is null) return null;
        if (TriggerKindReason(reaction, trigger) is { } kindReason) return kindReason;
        var source = context.Application.Triggers.SingleOrDefault(source => source.Id == trigger.Source);
        if (source is null) return $"Reaction '{reaction.Name}' refers to an unknown application trigger (Cratis/Stage#343).";
        if (Identifiers.ToPascalCase(reaction.Name) == "Handle")
            return $"Reaction '{reaction.Name}' collides with the generated occurrence handler method (Cratis/Stage#343).";
        if (reaction.Triggers.Length != 1)
            return $"Reaction '{reaction.Name}' combines application trigger '{source.Name}' with other triggers; the Cratis target renders one application trigger per reaction (Cratis/Stage#343).";
        return ReactionReason(reaction, trigger, source, context) ?? PayloadReason(source, context) ?? EffectsReason(reaction, trigger, source, context);
    }

    static string? EffectsReason(SemanticReaction reaction, SemanticReactionTrigger trigger, SemanticApplicationTrigger source, SemanticApplicationContext context) =>
        ProductionsReason(reaction, trigger, source, context) ?? InvocationsReason(reaction, trigger, source, context);

    static string? TriggerKindReason(SemanticReaction reaction, SemanticReactionTrigger trigger) => trigger.Kind switch
    {
        SemanticReactionTriggerKind.Startup or SemanticReactionTriggerKind.Shutdown => $"Reaction '{reaction.Name}' uses trigger kind '{trigger.Kind}'; the Cratis target does not render lifecycle reactions yet, because they need an explicit activation identity and namespace policy (Cratis/Stage#343).",
        SemanticReactionTriggerKind.Interval or SemanticReactionTriggerKind.Schedule => $"Reaction '{reaction.Name}' uses trigger kind '{trigger.Kind}'; the Cratis target does not render clock reactions until Chronicle schedules and due-time parity are proven (Cratis/Stage#343).",
        SemanticReactionTriggerKind.ApplicationTrigger => null,
        _ => $"Reaction '{reaction.Name}' uses unknown trigger kind '{trigger.Kind}', which the Cratis target does not render (Cratis/Stage#343)."
    };

    static string? ReactionReason(SemanticReaction reaction, SemanticReactionTrigger trigger, SemanticApplicationTrigger source, SemanticApplicationContext context)
    {
        if (HandlerCount(source, context) != 1)
            return $"Application trigger '{source.Name}' is handled by more than one reaction; Chronicle attaches one handler per trigger (Cratis/Stage#343).";
        var option = UnsupportedOption(reaction, trigger);
        return option is null ? null : $"Reaction '{reaction.Name}' uses '{option}' with application trigger '{source.Name}', which the Cratis target does not render yet (Cratis/Stage#343).";
    }

    static string? UnsupportedOption(SemanticReaction reaction, SemanticReactionTrigger trigger)
    {
        if (trigger.Where is not null) return "where";
        if (reaction.RunsAs is not null && trigger.Invokes.IsEmpty) return "runs as";
        return reaction.From is not null ? "from" : null;
    }

    static string? PayloadReason(SemanticApplicationTrigger source, SemanticApplicationContext context)
    {
        if (!GeneratedPascalCase.EventMembersAreUnique(source.Name, source.Properties.Select(property => property.Name)))
            return $"Application trigger '{source.Name}' has property names that collide in generated C# (Cratis/Stage#343).";
        foreach (var property in source.Properties)
        {
            if (Protected(property.Type, context, []))
                return $"Application trigger '{source.Name}' property '{property.Name}' is personal data; occurrence payloads must be plaintext without personal data (Cratis/Stage#343).";
            if (!SemanticCratisAdmission.TypeExists(context, property.Type) || property.Type.IsOptional)
                return $"Application trigger '{source.Name}' property '{property.Name}' has a type the Cratis target cannot preserve for an occurrence (Cratis/Stage#343).";
        }
        return null;
    }

    static bool Protected(SemanticTypeReference type, SemanticApplicationContext context, HashSet<SemanticId> visited)
    {
        if (type.Kind == SemanticTypeReferenceKind.Concept) return context.Concepts.TryGetValue(type.Target, out var concept) && concept.Compliance is not null;
        return type.Kind == SemanticTypeReferenceKind.CompositeType && visited.Add(type.Target) && context.Types.TryGetValue(type.Target, out var composite) &&
            composite.Properties.Any(property => Protected(property.Type, context, visited));
    }

    static string? ProductionsReason(SemanticReaction reaction, SemanticReactionTrigger trigger, SemanticApplicationTrigger source, SemanticApplicationContext context)
    {
        if (trigger.Produces.IsEmpty && trigger.Invokes.IsEmpty)
            return $"Reaction '{reaction.Name}' has no direct productions for application trigger '{source.Name}' (Cratis/Stage#343).";
        foreach (var produced in trigger.Produces)
        {
            var target = context.Events[produced.EventContract];
            if (produced.Destination is null)
                return $"Reaction '{reaction.Name}' produces '{target.Name}' without 'for'; an application trigger has no event source to default to (Cratis/Stage#343).";
            if (InvalidExpression(produced, source, context) is { } expression)
                return $"Reaction '{reaction.Name}' maps '{expression}' from application trigger '{source.Name}', which the Cratis target cannot preserve for an occurrence (Cratis/Stage#343).";
            if (!Portable(produced, target, source, context))
                return $"Reaction '{reaction.Name}' produces '{target.Name}' with a shape the Cratis target cannot preserve for an occurrence (Cratis/Stage#343).";
        }
        return null;
    }

    static string? InvalidExpression(SemanticProducedEvent produced, SemanticApplicationTrigger source, SemanticApplicationContext context)
    {
        if (!DestinationMatches(produced, source, context))
            return Describe(produced.Destination!);
        var target = context.Events[produced.EventContract];
        var invalid = produced.Mappings.FirstOrDefault(mapping => !MappingMatches(mapping, target, source, context));
        return invalid is null ? null : Describe(invalid.Source);
    }

    static bool DestinationMatches(SemanticProducedEvent produced, SemanticApplicationTrigger source, SemanticApplicationContext context) =>
        produced.DestinationType is { } type && new SemanticTypeSystem(context).SupportsReducerIdentifier(type) && ExpressionMatches(produced.Destination!, type, source, context);

    static bool MappingMatches(SemanticPropertyMapping mapping, SemanticEventContract target, SemanticApplicationTrigger source, SemanticApplicationContext context) =>
        target.Properties.FirstOrDefault(property => property.Id == mapping.TargetProperty) is { } property && ExpressionMatches(mapping.Source, property.Type, source, context);

    static string Describe(SemanticExpression expression) => expression switch
    {
        SemanticEventContextExpression member => $"$context.{char.ToLowerInvariant(member.Value.ToString()[0])}{member.Value.ToString()[1..]}",
        _ => expression.ToString() ?? expression.GetType().Name
    };

    static bool Portable(SemanticProducedEvent produced, SemanticEventContract target, SemanticApplicationTrigger source, SemanticApplicationContext context) =>
        IsUnconditional(produced) && target.Properties.All(property => PropertyMappedOnce(property, produced, source, context)) &&
        produced.Mappings.Length == target.Properties.Length && target.Tags.Concat(produced.Tags).Distinct(StringComparer.Ordinal).Count() == target.Tags.Length + produced.Tags.Length;

    static bool IsUnconditional(SemanticProducedEvent produced) => produced.Condition is null && produced.When is null && produced.Route is null;

    static bool PropertyMappedOnce(SemanticProperty property, SemanticProducedEvent produced, SemanticApplicationTrigger source, SemanticApplicationContext context) =>
        SemanticCratisAdmission.TypeExists(context, property.Type) && !property.Type.IsOptional &&
        produced.Mappings.Count(mapping => mapping.TargetProperty == property.Id && ExpressionMatches(mapping.Source, property.Type, source, context)) == 1;

    static bool ExpressionMatches(SemanticExpression expression, SemanticTypeReference type, SemanticApplicationTrigger source, SemanticApplicationContext context) => expression switch
    {
        SemanticResolvedExpression { Root: SemanticExpressionRootKind.Trigger, Source: SemanticExpressionSourceKind.Property } resolved => source.Properties.Any(property => property.Id == resolved.Target && property.Type == type),
        SemanticValueExpression value => ValueMatches(value.Value, type, context),
        _ => false
    };
}
