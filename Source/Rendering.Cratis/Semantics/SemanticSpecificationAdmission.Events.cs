// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Checks event expectations and property values.
/// Explicit null, member-order and record-equality refusals are renderer-only fidelity limits:
/// the executor and Host retain supplied semantic values and must not inherit these refusals.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    internal static bool TranslationValuesMatch(SemanticApplicationContext context, SemanticId contract, System.Collections.Immutable.ImmutableArray<SemanticPropertyValue> values) =>
        context.Events.TryGetValue(contract, out var declaration) && ValuesMatch(values, declaration.Properties, context: context);

    internal static bool TranslationSourceMatches(SemanticApplicationContext context, SemanticEventSourceIdentity source) =>
        IsScalar(source.Value) && IsLosslessEventSource(context, source.Type);

    static bool EventMatches(SemanticApplicationContext context, SemanticSpecificationEvent expected) =>
        context.Events.TryGetValue(expected.EventContract, out var @event) && ValuesMatch(expected.Values, @event.Properties);

    static bool HasRenderableGivenEvents(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.GivenEvents.All(given => (given.EventSource is { } source
            ? IsScalar(source.Value) && IsLosslessEventSource(context, source.Type)
            : !SemanticSpecificationEventSources.RequiresSource(context, given.EventContract, given.Route is not null)) && EventMatches(context, given));

    static bool GivenKeysMatchProjectedProperties(SemanticApplicationContext context, SemanticSpecification specification) =>
        specification.GivenEvents.All(given => context.Projections.Values
            .SelectMany(projection => projection.Transitions)
            .Where(transition => transition.EventContract == given.EventContract)
            .All(transition => transition.AffectedInstance.Key is SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event } key &&
                given.Values.SingleOrDefault(value => value.TargetProperty == key.Target)?.Value is { } propertyValue &&
                given.EventSource is { } source && Equals(propertyValue, source.Value)));

    static bool CommandValuesMatch(SemanticApplicationContext context, SemanticSpecification specification, SemanticCommand command) =>
        ValuesMatch(
            specification.When!.Values,
            [.. command.Properties.Where(property => !property.IsGenerated)],
            context: specification.ThenErrors.IsEmpty ? null : context);

    static bool ValuesMatch(
        System.Collections.Immutable.ImmutableArray<SemanticPropertyValue> values,
        System.Collections.Immutable.ImmutableArray<SemanticProperty> properties,
        bool exactly = true,
        SemanticApplicationContext? context = null) =>
        (!exactly || values.Length == properties.Length) &&
        values.Select(value => value.TargetProperty).Distinct().Count() == values.Length &&
        values.All(value =>
            properties.Any(property => value.TargetProperty == property.Id && IsCompatible(value.Value, property.Type, context)));

    static bool IsCompatible(SemanticValue value, SemanticTypeReference type, SemanticApplicationContext? context) => value switch
    {
        SemanticNullValue => type.IsOptional,
        _ when type.IsCollection => CollectionValuesMatch(value, type, context),
        SemanticCompositeValue composite when type.Kind == SemanticTypeReferenceKind.CompositeType => IsComparableCompositeValue(composite, type, context),
        _ => IsScalar(value)
    };

    static bool CollectionValuesMatch(SemanticValue value, SemanticTypeReference type, SemanticApplicationContext? context) =>
        value is SemanticArrayValue array && array.Values.All(element => IsCompatible(element, type with { IsCollection = false, IsOptional = false }, context));

    static bool IsComparableCompositeValue(SemanticCompositeValue value, SemanticTypeReference type, SemanticApplicationContext? context)
    {
        // The reference compares composites structurally, including omitted members; C# record equality does not
        // distinguish omission from explicit null, so refuse explicit null members even in typed fixtures.
        // Record equality also compares collection-valued members by reference, so keep those unsupported.
        return value.Properties.All(property => HasComparableValue(property.Value)) &&
            (context is null || CompositeMembersFollowDeclarationOrder(value, context.Types[type.Target], context));
    }

    static bool CompositeMembersFollowDeclarationOrder(SemanticCompositeValue value, SemanticCompositeType type, SemanticApplicationContext context)
    {
        var supplied = value.Properties.Select(property => property.TargetProperty).ToArray();
        var declared = type.Properties.Where(property => supplied.Contains(property.Id)).Select(property => property.Id);
        return supplied.SequenceEqual(declared) && value.Properties.All(property =>
            IsCompatible(property.Value, type.Properties.Single(member => member.Id == property.TargetProperty).Type, context));
    }

    static bool HasComparableValue(SemanticValue value) => value switch
    {
        SemanticCompositeValue composite => composite.Properties.All(property => HasComparableValue(property.Value)),
        _ => IsScalar(value)
    };

    static bool IsScalar(SemanticValue value) => value is SemanticTextValue or SemanticNumberValue or SemanticBooleanValue;
}
