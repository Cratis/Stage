// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders keyed instance and property assertions for read-model specifications.
/// </summary>
internal static class SemanticReadModelSpecificationExpectations
{
    /// <summary>
    /// Renders the expected instance and its property assertions.
    /// </summary>
    /// <param name="scope">The shared source-rendering state.</param>
    /// <param name="expectation">The resolved read-model expectation.</param>
    /// <returns>Whether the rendered values require the Common namespace.</returns>
    internal static bool Render(SemanticReadModelSpecificationScope scope, Expectation expectation)
    {
        var (readModel, expected, _, absentIndex) = expectation;
        var builder = scope.Builder;
        var types = scope.Types;
        var keyProperties = readModel.Properties.Where(property => property.IsIdentifier).ToArray();
        var keyProperty = keyProperties[0];
        var needsCommon = expected.Key is SemanticCompositeValue compositeKey
            ? keyProperties.Any(property => SemanticTypeSystem.ValueNeedsCommon(compositeKey.Properties.Single(part => part.TargetProperty == property.Id).Value, property.Type))
            : SemanticTypeSystem.ValueNeedsCommon(expected.Key, keyProperty.Type);
        var instance = expected.Key is SemanticCompositeValue composite
            ? CompositeInstance(readModel, composite, types)
            : $"_scenario.InstanceForEventSourceId({types.EventSourceExpression(types.Value(expected.Key, keyProperty.Type), keyProperty.Type)})!";
        if (absentIndex is not null)
        {
            var key = types.EventSourceExpression(types.Value(expected.Key, keyProperty.Type), keyProperty.Type);
            builder.Line($"[global::Xunit.FactAttribute] void should_not_project_the_absent_instance() => _scenario.Instances.ContainsKey({key}).ShouldBeFalse();");
        }
        else if (expected.Values.IsEmpty)
        {
            builder.Line($"[global::Xunit.FactAttribute] void should_project_the_expected_instance() => {instance}.ShouldNotBeNull();");
        }

        needsCommon |= RenderProperties(scope, expectation, instance);
        return needsCommon;
    }

    static bool RenderProperties(SemanticReadModelSpecificationScope scope, Expectation expectation, string instance)
    {
        var builder = scope.Builder;
        var types = scope.Types;
        var needsCommon = false;
        foreach (var value in expectation.Expected.Values.OrderBy(value => value.TargetProperty.ToString(), StringComparer.Ordinal))
        {
            var property = expectation.ReadModel.Properties.Single(_ => _.Id == value.TargetProperty);
            if (property.Type.IsCollection && value.Value is SemanticArrayValue array)
            {
                var elementType = property.Type with { IsCollection = false, IsOptional = false };
                needsCommon |= SemanticTypeSystem.DeclarationNeedsCommon(elementType) ||
                    SemanticTypeSystem.ValueNeedsCommon(value.Value, property.Type);
                var elements = string.Join(", ", array.Values.Select(element => types.Value(element, elementType)));
                var name = Identifiers.ToSnakeCase(property.Name);
                var optionalAccess = expectation.IsReduced ? "!.Value" : "!";
                builder.Line($"static readonly {types.Type(elementType)}[] _expected_{name} = [{elements}];")
                    .Line($"[global::Xunit.FactAttribute] void should_project_{name}() => " +
                        $"global::Xunit.Assert.True(global::System.Linq.Enumerable.SequenceEqual({instance}.{Identifiers.ToPascalCase(property.Name)}" +
                        (property.Type.IsOptional ? optionalAccess : string.Empty) + $", _expected_{name}));");
            }
            else
            {
                needsCommon |= SemanticTypeSystem.ValueNeedsCommon(value.Value, property.Type);
                builder.Line(
                    $"[global::Xunit.FactAttribute] void should_project_{Identifiers.ToSnakeCase(property.Name)}() => " +
                    $"{instance}.{Identifiers.ToPascalCase(property.Name)}.ShouldEqual({types.Value(value.Value, property.Type)});");
            }
        }

        return needsCommon;
    }

    static string CompositeInstance(SemanticReadModel model, SemanticCompositeValue key, SemanticTypeSystem types)
    {
        var parts = model.Properties.Where(property => property.IsIdentifier).OrderBy(property => property.Id.ToString(), StringComparer.Ordinal);
        var values = parts.Select(property => types.Value(key.Properties.Single(part => part.TargetProperty == property.Id).Value, property.Type));
        var keyType = types.SliceType(model.Id, Projections.SemanticCompositeKeyAdmission.KeyTypeName(model));
        return $"global::System.Linq.Enumerable.SingleOrDefault(_scenario.Instances.Values, instance => instance.Id == new {keyType}({string.Join(", ", values)}))!";
    }

    /// <summary>
    /// Groups the model, expected state, observation kind and optional absent-instance position.
    /// </summary>
    /// <param name="ReadModel">The resolved read model.</param>
    /// <param name="Expected">The expected read-model state.</param>
    /// <param name="IsReduced">Whether a reducer builds the model.</param>
    /// <param name="AbsentIndex">The absent-instance position, when applicable.</param>
    internal sealed record Expectation(
        SemanticReadModel ReadModel,
        SemanticSpecificationReadModel Expected,
        bool IsReduced,
        int? AbsentIndex);
}
