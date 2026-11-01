// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits event-triggered Automation slices without dropping their own artifacts.
/// </summary>
internal static partial class SemanticCratisAdmission
{
    static void ValidateAutomation(SemanticApplicationContext context, SemanticSlice slice, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var command in slice.Commands)
        {
            ValidateCommand(context, slice, command, diagnostics);
        }
        if (SemanticReadSurface.IsDeclaredBy(slice))
        {
            ValidateStateView(context, slice, diagnostics);
        }
        foreach (var reaction in slice.Reactions.IsDefault ? [] : slice.Reactions)
        {
            ValidateReaction(context, slice, reaction, diagnostics);
        }
        if (slice.Events.Any(@event => !ReactionEventIsRenderable(context, @event)))
        {
            diagnostics.Add(Error("STAGE-ESM-039", $"Automation slice '{slice.Name}' declares event properties the Cratis target cannot preserve.", slice.Id));
        }
    }

    static void ValidateReaction(SemanticApplicationContext context, SemanticSlice slice, SemanticReaction reaction, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (reaction.Triggers.IsEmpty)
        {
            diagnostics.Add(Error("STAGE-ESM-039", $"Reaction '{reaction.Name}' has no event subscription for a Chronicle reactor.", reaction.Id));
            return;
        }
        ValidateReactionIdentity(context, reaction, diagnostics);
        ValidateRefusalValues(context, reaction, diagnostics);
        if (reaction.Triggers.GroupBy(trigger => trigger.Source).Any(group => group.Count() > 1 &&
            group.Any(trigger => trigger.Invokes.Any(invocation => !invocation.OnRefused.IsEmpty))))
        {
            diagnostics.Add(Error("STAGE-ESM-039", $"Reaction '{reaction.Name}' has refusal branches across same-source triggers; Arc stops a returned invocation list, not one trigger subgroup.", reaction.Id));
        }
        if (ReactionNamesCollide(context, slice, reaction))
        {
            diagnostics.Add(Error("STAGE-ESM-012", $"Reaction '{reaction.Name}' has event handler names that collide in generated C#.", reaction.Id));
        }
        foreach (var trigger in reaction.Triggers.Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event && !ReactionTriggerIsRenderable(context, trigger)))
        {
            diagnostics.Add(Error("STAGE-ESM-039", $"Reaction '{reaction.Name}' cannot be rendered without changing its condition, destination or mappings.", reaction.Id));
        }
    }

    static bool ReactionNamesCollide(SemanticApplicationContext context, SemanticSlice slice, SemanticReaction reaction) =>
        SliceNaming.FileName(slice.Name) == $"{GeneratedPascalCase.From(reaction.Name)}.cs" ||
        ReactionHandlerMatchesClass(context, reaction) || ReactionHandlerNamesCollide(context, reaction);

    static bool ReactionHandlerMatchesClass(SemanticApplicationContext context, SemanticReaction reaction) =>
        reaction.Triggers.Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event && context.Events.ContainsKey(trigger.Source))
            .GroupBy(trigger => trigger.Source).Any(group =>
            GeneratedPascalCase.From(context.Events[group.Key].Name) == GeneratedPascalCase.From(reaction.Name));

    static bool ReactionHandlerNamesCollide(SemanticApplicationContext context, SemanticReaction reaction) =>
        reaction.Triggers.Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event && context.Events.ContainsKey(trigger.Source))
            .Select(trigger => trigger.Source).Distinct().Select(id => GeneratedPascalCase.From(context.Events[id].Name))
            .GroupBy(name => name, StringComparer.Ordinal).Any(group => group.Count() > 1);

    static bool ReactionTriggerIsRenderable(SemanticApplicationContext context, SemanticReactionTrigger trigger)
    {
        if (!context.Events.TryGetValue(trigger.Source, out var source)) return false;

        return ReactionConditionIsRenderable(context, source, trigger.Where) &&
            trigger.Produces.All(produced => CanRenderProduction(context, source, produced)) &&
            trigger.Invokes.All(invocation => CanRenderInvocation(context, source, invocation));
    }

    static bool ReactionConditionIsRenderable(SemanticApplicationContext context, SemanticEventContract source, SemanticCondition? condition) =>
        condition is null || SemanticConditionRendering.CanRender(condition, source.Properties, context, numericOrderingOnly: true);

    static bool CanRenderProduction(SemanticApplicationContext context, SemanticEventContract source, SemanticProducedEvent produced, SemanticInvocationRefusal? refusal = null)
    {
        if (!context.Events.TryGetValue(produced.EventContract, out var target)) return false;

        return HasPortableReactionProduction(produced) && ReactionEventIsRenderable(context, target) &&
            ReactionMappingCountMatches(target, produced) && ReactionTagsAreDistinct(target, produced) &&
            ReactionMappingsMatch(context, source, target, produced, refusal) && ReactionDestinationMatches(context, source, produced);
    }

    static bool HasPortableReactionProduction(SemanticProducedEvent produced) =>
        produced.Condition is null && produced.When is null && produced.Route is null;

    static bool ReactionEventIsRenderable(SemanticApplicationContext context, SemanticEventContract @event) =>
        @event.Properties.All(property => TypeExists(context, property.Type, @event.Id) && !property.Type.IsOptional);

    static bool ReactionMappingCountMatches(SemanticEventContract target, SemanticProducedEvent produced) =>
        produced.Mappings.Length == target.Properties.Length;

    static bool ReactionTagsAreDistinct(SemanticEventContract target, SemanticProducedEvent produced) =>
        target.Tags.Concat(produced.Tags).Distinct(StringComparer.Ordinal).Count() == target.Tags.Length + produced.Tags.Length;

    static bool ReactionMappingsMatch(SemanticApplicationContext context, SemanticEventContract source, SemanticEventContract target, SemanticProducedEvent produced, SemanticInvocationRefusal? refusal) =>
        target.Properties.All(property => produced.Mappings.Any(mapping => mapping.TargetProperty == property.Id &&
            (ReactionExpressionMatches(context, source, mapping.Source, property.Type) || RefusalExpressionMatches(mapping.Source, property.Type, refusal))));

    static bool RefusalExpressionMatches(SemanticExpression expression, SemanticTypeReference target, SemanticInvocationRefusal? refusal) =>
        refusal is not null && expression is SemanticRefusalExpression value && value.Type == target &&
        target is { Kind: SemanticTypeReferenceKind.Primitive, Primitive: SemanticPrimitiveType.Text, IsCollection: false, IsOptional: false } &&
        (value.Member == "reason" || value.Member == "message" || (value.Member == "constraint" && refusal.Selector == "constraint"));

    static bool ReactionDestinationMatches(SemanticApplicationContext context, SemanticEventContract source, SemanticProducedEvent produced) =>
        produced.Destination is null || (produced.DestinationType is { IsCollection: false, IsOptional: false } type &&
            ReactionExpressionMatches(context, source, produced.Destination, type));

    static bool CanRenderInvocation(SemanticApplicationContext context, SemanticEventContract source, SemanticInvocation invocation)
    {
        if (!context.Commands.TryGetValue(invocation.Command, out var command)) return false;

        return invocation.OnRefused.All(branch =>
            (branch.Selector == "any" || branch.Selector == "validation" || branch.Selector == "authorization" || branch.Selector == "constraint") &&
            (branch.Constraint is null || branch.Selector == "constraint") &&
            branch.Produces.All(produced => CanRenderProduction(context, source, produced, branch))) &&
            command.Properties.Where(property => !property.IsGenerated).All(property =>
            invocation.Mappings.FirstOrDefault(mapping => mapping.TargetProperty == property.Id) is { } mapping
                ? ReactionExpressionMatches(context, source, mapping.Source, property.Type)
                : property.Type.IsOptional);
    }

    static bool ReactionExpressionMatches(SemanticApplicationContext context, SemanticEventContract source, SemanticExpression expression, SemanticTypeReference target) =>
        TypeExists(context, target) && expression switch
        {
            SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event, Source: SemanticExpressionSourceKind.Property } resolved =>
                source.Properties.Any(property => property.Id == resolved.Target && property.Type == target),
            SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred } occurrence => occurrence.Type == target,
            SemanticEventContextExpression { Value: SemanticEventContextValueKind.EventSourceIdentity } identity => identity.Type == target && new SemanticTypeSystem(context).SupportsReducerIdentifier(target),
            SemanticValueExpression value => ReactionValueMatches(context, value.Value, target),
            _ => false
        };

    static bool ReactionValueMatches(SemanticApplicationContext context, SemanticValue value, SemanticTypeReference type)
    {
        if (value is SemanticNullValue) return type.IsOptional;
        if (type.IsCollection) return ReactionCollectionValueMatches(context, value, type);
        if (type.Kind == SemanticTypeReferenceKind.CompositeType) return ReactionCompositeValueMatches(context, value, type);
        if (IsReactionEnumeration(context, type)) return ReactionEnumerationValueMatches(context, value, type);

        return ReactionPrimitiveValueMatches(context, value, SemanticValidationRendering.UnderlyingPrimitive(type, context));
    }

    static bool ReactionCollectionValueMatches(SemanticApplicationContext context, SemanticValue value, SemanticTypeReference type) =>
        value is SemanticArrayValue array && array.Values.All(element => ReactionValueMatches(context, element, type with { IsCollection = false, IsOptional = false }));

    static bool ReactionCompositeValueMatches(SemanticApplicationContext context, SemanticValue value, SemanticTypeReference type) =>
        value is SemanticCompositeValue composite &&
        context.Types[type.Target].Properties.All(property => composite.Properties.Any(item => item.TargetProperty == property.Id && ReactionValueMatches(context, item.Value, property.Type)));

    static bool IsReactionEnumeration(SemanticApplicationContext context, SemanticTypeReference type) =>
        type.Kind == SemanticTypeReferenceKind.Concept && !context.Concepts[type.Target].Values.IsEmpty;

    static bool ReactionEnumerationValueMatches(SemanticApplicationContext context, SemanticValue value, SemanticTypeReference type) =>
        value is SemanticTextValue member && context.Concepts[type.Target].Values.Contains(member.Value, StringComparer.Ordinal);

    static bool ReactionPrimitiveValueMatches(SemanticApplicationContext context, SemanticValue value, SemanticPrimitiveType primitive) => (value, primitive) switch
    {
        (SemanticTextValue, SemanticPrimitiveType.Text) => true,
        (SemanticTextValue text, SemanticPrimitiveType.Uuid) => Guid.TryParse(text.Value, out _),
        (SemanticTextValue text, SemanticPrimitiveType.Date) => DateOnly.TryParse(text.Value, System.Globalization.CultureInfo.InvariantCulture, out _),
        (SemanticTextValue text, SemanticPrimitiveType.DateTime) => DateTimeOffset.TryParse(text.Value, System.Globalization.CultureInfo.InvariantCulture, out _),
        (SemanticNumberValue number, SemanticPrimitiveType.WholeNumber) => ReactionWholeNumberValueMatches(context, number),
        (SemanticNumberValue, SemanticPrimitiveType.DecimalNumber) => true,
        (SemanticBooleanValue, SemanticPrimitiveType.Boolean) => true,
        _ => false
    };

    static bool ReactionWholeNumberValueMatches(SemanticApplicationContext context, SemanticNumberValue number) =>
        number.Value == decimal.Truncate(number.Value) && ReactionWholeNumberIsInRange(context, number.Value);

    static bool ReactionWholeNumberIsInRange(SemanticApplicationContext context, decimal value) =>
        context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V8)
            ? value is >= long.MinValue and <= long.MaxValue
            : value is >= int.MinValue and <= int.MaxValue;
}
