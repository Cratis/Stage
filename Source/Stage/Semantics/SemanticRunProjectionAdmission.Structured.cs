// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Admits per-run projections that copy a whole composite or collection from an event property, or read a scalar
/// from a nested event path, when Chronicle can carry the value without losing or retyping it.
/// </summary>
internal static partial class SemanticRunProjectionAdmission
{
    const string IncompleteStructuredValue = "A structured value copied or read by a projection must be complete (every member present, none null): Chronicle keeps the event's JSON as is, so a missing member cannot be distinguished from a default.";

    // A whole-value copy keeps one closed type end to end: no optional members, no DateTime, recursively.
    static bool WholeValueCopy(SemanticExecutionPlan plan, SemanticProperty property, SemanticProjectionScope? scope)
    {
        if (scope is null || property.IsIdentifier || !ClosedType(plan, property.Type with { IsOptional = false }, []))
        {
            return false;
        }

        var targeting = scope.From.Select(from => (Event: (SemanticId?)from.EventContract, from.Mappings))
            .Concat(scope.Joins.Select(join => (Event: (SemanticId?)join.EventContract, join.Mappings)))
            .Concat(scope.Every is { } every ? [(Event: null, every.Mappings)] : [])
            .SelectMany(block => block.Mappings.Where(mapping => mapping.Target.Length > 0 && mapping.Target[0] == property.Id)
                .Select(mapping => (block.Event, Mapping: mapping)))
            .ToArray();

        return targeting.Length > 0 && scope.Nested.IsEmpty && targeting.All(entry =>
            entry.Event is { } eventContract &&
            entry.Mapping is { Operation: SemanticProjectionOperation.Set, Factor: null, Target.Length: 1, Source: SemanticProjectionEventProperty { Path: [var id] } } &&
            plan.Events[eventContract].Properties.SingleOrDefault(candidate => candidate.Id == id) is { Type.IsOptional: false } source &&
            source.Type.Kind == property.Type.Kind && source.Type.Target == property.Type.Target && source.Type.IsCollection == property.Type.IsCollection);
    }

    static bool ClosedType(SemanticExecutionPlan plan, SemanticTypeReference type, HashSet<SemanticId> visiting)
    {
        if (type.IsOptional)
        {
            return false;
        }

        var element = type with { IsCollection = false };
        if (element.Kind != SemanticTypeReferenceKind.CompositeType)
        {
            return IsScalar(element) && Primitive(plan, element) != SemanticPrimitiveType.DateTime;
        }

        if (!visiting.Add(element.Target))
        {
            return false;
        }

        var properties = plan.Model.Application.Types.Single(candidate => candidate.Id == element.Target).Properties;
        var closed = properties.Length > 0 && ChronicleReadModelPropertyNamesAreSafe.Check(properties, rootLevel: false) &&
            SemanticRunAdmission.RuntimePropertyNamesAreUnique(properties) &&
            properties.All(property => ClosedType(plan, property.Type, visiting));
        visiting.Remove(element.Target);
        return closed;
    }

    // A nested event path reads one required, non-collection scalar through required composites.
    static bool NestedPathSupported(SemanticExecutionPlan plan, SemanticEventContract @event, SemanticProjectionEventProperty source)
    {
        var properties = @event.Properties;
        for (var index = 0; index < source.Path.Length; index++)
        {
            if (properties.SingleOrDefault(candidate => candidate.Id == source.Path[index]) is not { Type: { IsOptional: false, IsCollection: false } } member)
            {
                return false;
            }
            if (index == source.Path.Length - 1)
            {
                return IsScalar(member.Type) && Primitive(plan, member.Type) != SemanticPrimitiveType.DateTime;
            }
            if (member.Type.Kind != SemanticTypeReferenceKind.CompositeType)
            {
                return false;
            }
            properties = plan.Model.Application.Types.Single(candidate => candidate.Id == member.Type.Target).Properties;
        }
        return false;
    }

    // Every structured value that the projection copies or reads through a nested path must arrive complete.
    static string? StructuredValueRefusal(SemanticExecutionPlan plan, SemanticProjectionScope scope, SemanticSpecification specification)
    {
        var sources = scope.From.Select(from => (from.EventContract, from.Mappings))
            .Concat(scope.Joins.Select(join => (join.EventContract, join.Mappings)))
            .SelectMany(block => block.Mappings.Select(mapping => (block.EventContract, Mapping: mapping)))
            .Where(entry => entry.Mapping.Source is SemanticProjectionEventProperty { Path: var path } &&
                (path.Length > 1 || IsStructured(plan.Events[entry.EventContract].Properties.Single(property => property.Id == path[0]).Type)))
            .Select(entry => (entry.EventContract, Source: (SemanticProjectionEventProperty)entry.Mapping.Source!))
            .ToArray();
        if (sources.Length == 0)
        {
            return null;
        }

        bool Incomplete(SemanticId eventContract, Func<SemanticId, SemanticValue?> valueOf) =>
            sources.Any(entry => entry.EventContract == eventContract && !ValueComplete(plan, entry.Source.Path, valueOf(entry.Source.Path[0]), plan.Events[eventContract].Properties));

        var incomplete = specification.GivenEvents.Any(given => Incomplete(given.EventContract, id => given.Values.SingleOrDefault(value => value.TargetProperty == id)?.Value)) ||
            (specification.WhenAppended is { } appended && Incomplete(appended.EventContract, id => appended.Values.SingleOrDefault(value => value.TargetProperty == id)?.Value)) ||
            ProducedIncomplete(plan, specification, Incomplete);
        return incomplete ? IncompleteStructuredValue : null;
    }

    static bool ProducedIncomplete(SemanticExecutionPlan plan, SemanticSpecification specification, Func<SemanticId, Func<SemanticId, SemanticValue?>, bool> incomplete)
    {
        if (specification.When is not { } when || !plan.Commands.TryGetValue(when.Command, out var command) || !specification.ThenErrors.IsEmpty || specification.ThenDenied)
        {
            return false;
        }

        return command.Produces.Any(produced => incomplete(produced.EventContract, id => produced.Mappings.SingleOrDefault(mapping => mapping.TargetProperty == id)?.Source switch
        {
            SemanticResolvedExpression { Root: SemanticExpressionRootKind.Command, Source: SemanticExpressionSourceKind.Property } property =>
                when.Values.Concat(when.GeneratedValues).SingleOrDefault(input => input.TargetProperty == property.Target)?.Value,
            SemanticValueExpression literal => literal.Value,
            _ => null
        }));
    }

    static bool IsStructured(SemanticTypeReference type) => type.IsCollection || type.Kind == SemanticTypeReferenceKind.CompositeType;

    // A path of one id means the whole property value; a longer path needs only its leaf.
    static bool ValueComplete(SemanticExecutionPlan plan, ImmutableArray<SemanticId> path, SemanticValue? value, ImmutableArray<SemanticProperty> properties)
    {
        var type = properties.Single(property => property.Id == path[0]).Type;
        if (path.Length == 1)
        {
            return value is not null && Complete(plan, value, type);
        }

        for (var index = 1; index < path.Length; index++)
        {
            if (value is not SemanticCompositeValue composite || composite.Properties.SingleOrDefault(candidate => candidate.TargetProperty == path[index]) is not { } member)
            {
                return false;
            }

            value = member.Value;
        }
        return value is not null and not SemanticNullValue;
    }

    static bool Complete(SemanticExecutionPlan plan, SemanticValue value, SemanticTypeReference type)
    {
        if (value is SemanticNullValue)
        {
            return false;
        }
        if (type.IsCollection)
        {
            return value is SemanticArrayValue array && array.Values.All(element => Complete(plan, element, type with { IsCollection = false }));
        }
        if (type.Kind != SemanticTypeReferenceKind.CompositeType)
        {
            return true;
        }

        return value is SemanticCompositeValue composite && plan.Model.Application.Types.Single(candidate => candidate.Id == type.Target).Properties
            .All(property => composite.Properties.SingleOrDefault(item => item.TargetProperty == property.Id) is { } item && Complete(plan, item.Value, property.Type));
    }

    static bool SeedStructuredIncomplete(SemanticExecutionPlan plan, SemanticReadModel model, IEnumerable<SemanticPropertyValue> seed)
    {
        foreach (var property in model.Properties)
        {
            if (!IsStructured(property.Type)) continue;
            var value = seed.SingleOrDefault(candidate => candidate.TargetProperty == property.Id);
            if (value is null || !Complete(plan, value.Value, property.Type with { IsOptional = false })) return true;
        }
        return false;
    }
}
