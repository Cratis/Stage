// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Preserves the int32 schema contract of models before ESM v8.
/// </summary>
internal static partial class SemanticCratisAdmission
{
    static void ValidateWholeNumberLiterals(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V8)) return;

        foreach (var concept in context.Application.Concepts.Where(concept => concept.Primitive == SemanticPrimitiveType.WholeNumber))
        {
            foreach (var rule in concept.Validations) Check(rule.Operand, SemanticTypeReference.ForPrimitive(concept.Primitive), concept.Id);
        }

        foreach (var slice in slices.Select(located => located.Slice))
        {
            foreach (var command in slice.Commands)
            {
                foreach (var rule in command.Validations)
                {
                    var property = command.Properties.SingleOrDefault(property => property.Id == rule.Property);
                    if (property is not null) Check(rule.Operand, property.Type, command.Id);
                }
                foreach (var requirement in command.Requirements) CheckCondition(requirement.Condition, command);
                if (command.Destination is { Value: SemanticValueExpression value } destination)
                {
                    Check(value.Value, destination.Type, command.Id);
                }

                foreach (var produced in command.Produces.Where(produced => context.Events.ContainsKey(produced.EventContract)))
                {
                    var properties = context.Events[produced.EventContract].Properties;
                    foreach (var mapping in produced.Mappings.Where(mapping => mapping.Source is SemanticValueExpression))
                    {
                        var property = properties.SingleOrDefault(property => property.Id == mapping.TargetProperty);
                        if (property is not null) Check(((SemanticValueExpression)mapping.Source).Value, property.Type, command.Id);
                    }
                }
            }

            foreach (var projection in slice.Projections.Where(projection => projection.Scope is not null))
            {
                CheckScope(projection.Scope!, context.ReadModels[projection.ReadModel].Properties, projection.Id);
            }

            foreach (var specification in slice.Specifications)
            {
                if (specification.When is { } action)
                {
                    CheckProperties(action.Values, context.Commands[action.Command].Properties, specification.Id);
                    CheckIdentity(action.EventSource, specification.Id);
                }
                foreach (var occurrence in specification.GivenEvents.Concat(specification.ThenEvents))
                {
                    CheckProperties(occurrence.Values, context.Events[occurrence.EventContract].Properties, specification.Id);
                    CheckIdentity(occurrence.EventSource, specification.Id);
                }
                foreach (var expected in specification.GivenReadModels.Concat(specification.ThenReadModels).Concat(specification.ThenQueries.SelectMany(query => query.Results)))
                {
                    var properties = context.ReadModels[expected.ReadModel].Properties;
                    CheckProperties(expected.Values, properties, specification.Id);
                    Check(expected.Key, properties.Single(property => property.IsIdentifier).Type, specification.Id);
                }
                foreach (var query in specification.ThenQueries)
                {
                    if (context.Queries[query.Query].Argument is { } argument) Check(query.Key, argument.Type, specification.Id);
                }
            }
        }

        void Check(SemanticValue? value, SemanticTypeReference type, SemanticId artifact)
        {
            if (value is SemanticArrayValue array)
            {
                foreach (var element in array.Values) Check(element, type with { IsCollection = false }, artifact);
            }
            else if (value is SemanticCompositeValue composite && type.Kind == SemanticTypeReferenceKind.CompositeType)
            {
                CheckProperties(composite.Properties, context.Types[type.Target].Properties, artifact);
            }
            else if (value is SemanticNumberValue number && decimal.Truncate(number.Value) == number.Value &&
                (number.Value < int.MinValue || number.Value > int.MaxValue) &&
                SemanticValidationRendering.UnderlyingPrimitive(type, context) == SemanticPrimitiveType.WholeNumber)
            {
                diagnostics.Add(Error("STAGE-ESM-031", "A whole-number literal is outside the C# int range for this model. ESM v8 or later renders whole numbers as long; upgrading an existing event schema requires a new Chronicle generation.", artifact));
            }
        }

        void CheckProperties(IEnumerable<SemanticPropertyValue> values, IEnumerable<SemanticProperty> properties, SemanticId artifact)
        {
            foreach (var value in values)
            {
                var property = properties.SingleOrDefault(property => property.Id == value.TargetProperty);
                if (property is not null) Check(value.Value, property.Type, artifact);
            }
        }

        void CheckIdentity(SemanticEventSourceIdentity? identity, SemanticId artifact)
        {
            if (identity is not null) Check(identity.Value, identity.Type, artifact);
        }

        void CheckCondition(SemanticCondition condition, SemanticCommand command)
        {
            if (condition is SemanticLogicalCondition logical)
            {
                CheckCondition(logical.Left, command);
                CheckCondition(logical.Right, command);
            }
            else if (condition is SemanticComparison comparison)
            {
                var left = command.Properties.SingleOrDefault(property => property.Id == comparison.Left.Property);
                var right = command.Properties.SingleOrDefault(property => property.Id == comparison.Right.Property);
                if (left is not null) Check(comparison.Right.Value, left.Type, command.Id);
                if (right is not null) Check(comparison.Left.Value, right.Type, command.Id);
            }
        }

        void CheckScope(SemanticProjectionScope scope, IEnumerable<SemanticProperty> properties, SemanticId artifact)
        {
            foreach (var mapping in scope.From.SelectMany(from => from.Mappings).Concat(scope.Joins.SelectMany(join => join.Mappings)).Concat(scope.Every?.Mappings ?? []))
            {
                var candidates = properties;
                SemanticProperty? target = null;
                foreach (var part in mapping.Target)
                {
                    target = candidates.SingleOrDefault(property => property.Id == part);
                    candidates = target?.Type.Kind == SemanticTypeReferenceKind.CompositeType ? context.Types[target.Type.Target].Properties : [];
                }
                if (target is not null && mapping.Source is SemanticProjectionLiteral literal) Check(literal.Value, target.Type, artifact);
            }
            foreach (var nested in scope.Nested)
            {
                var property = properties.Single(property => property.Id == nested.Property);
                CheckScope(nested.Scope, context.Types[property.Type.Target].Properties, artifact);
            }
            foreach (var children in scope.Children)
            {
                var property = properties.Single(property => property.Id == children.Property);
                CheckScope(children.Scope, context.Types[property.Type.Target].Properties, artifact);
            }
        }
    }
}
