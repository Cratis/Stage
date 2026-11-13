// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// The gates a model places on its parts and the Choreography expressions they render to.
/// </summary>
internal static partial class SemanticAvailabilityRendering
{
    /// <summary>
    /// Collects the gates of the model: module, feature and slice gates by namespace, command and query gates by type.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <returns>The gates, one per target.</returns>
    internal static IReadOnlyList<SemanticAvailabilityGate> Gates(SemanticApplicationContext context)
    {
        var gates = new Dictionary<string, SemanticAvailabilityGate>(StringComparer.Ordinal);
        foreach (var located in context.AllSlices)
        {
            var modulePrefix = located.Path.Count - located.FeaturePath.Count - 1;
            Add(gates, SliceNaming.Namespace(context.RootNamespace, located.Path.Take(modulePrefix)), false, located.Module.Available, $"module '{located.Module.Name}'");
            for (var index = 0; index < located.FeaturePath.Count; index++)
            {
                Add(gates, SliceNaming.Namespace(context.RootNamespace, located.Path.Take(modulePrefix + index + 1)), false, located.FeaturePath[index].Available, $"feature '{located.FeaturePath[index].Name}'");
            }

            var sliceNamespace = SliceNaming.Namespace(context.RootNamespace, located.Path);
            Add(gates, sliceNamespace, false, located.Slice.Available, $"slice '{located.Slice.Name}'");
            foreach (var command in located.Slice.Commands)
            {
                Add(gates, $"{sliceNamespace}.{Identifiers.ToPascalCase(command.Name)}", true, command.Available, $"command '{command.Name}'");
            }

            foreach (var query in located.Slice.Queries)
            {
                Add(gates, $"{sliceNamespace}.{Identifiers.ToPascalCase(context.ReadModels[query.ReadModel].Name)}", true, query.Available, $"query '{query.Name}'");
            }
        }

        return [.. gates.Values];
    }

    static void Add(Dictionary<string, SemanticAvailabilityGate> gates, string key, bool isType, ImmutableArray<SemanticAvailabilityRequirement> requirements, string description)
    {
        if (requirements.IsDefaultOrEmpty) return;
        gates[key] = new(key, isType, requirements, description);
    }

    static string Expression(SemanticAvailabilityRequirement requirement) => requirement switch
    {
        SemanticAudienceRequirement audience => $"{Engine}.AvailabilityExpression.For({CSharpCodeBuilder.StringLiteral(audience.Audience)})",
        SemanticAvailabilityReference reference => $"{Engine}.AvailabilityExpression.With({CSharpCodeBuilder.StringLiteral(reference.Availability)})",
        SemanticConditionRequirement condition => Condition(condition.Condition),
        SemanticLogicalAvailability logical => Combine(logical.Operator, Expression(logical.Left), Expression(logical.Right)),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticAvailabilityRequirement), requirement.GetType().Name)
    };

    static string Condition(SemanticAvailabilityCondition condition) => condition switch
    {
        SemanticAvailabilityComparison comparison => $"{Engine}.AvailabilityExpression.When({Comparison(comparison)})",
        SemanticLogicalAvailabilityCondition logical => Combine(logical.Operator, Condition(logical.Left), Condition(logical.Right)),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticAvailabilityCondition), condition.GetType().Name)
    };

    static string Combine(SemanticLogicalOperator @operator, string left, string right) =>
        $"{Engine}.AvailabilityExpression.{(@operator == SemanticLogicalOperator.And ? "AllOf" : "AnyOf")}({left}, {right})";

    static string Comparison(SemanticAvailabilityComparison comparison)
    {
        var path = CSharpCodeBuilder.StringLiteral(comparison.Path);
        var literal = comparison.Operand.Value ?? throw UnsupportedSemanticRendering.For(nameof(SemanticContextOperand), comparison.Operand.Path ?? "empty");
        if (comparison.Operator == SemanticComparisonOperator.Equal && literal is SemanticBooleanValue { Value: true })
        {
            return $"new {Engine}.ContextComparison({path}, {Engine}.ComparisonOperator.IsTrue)";
        }

        return $"new {Engine}.ContextComparison({path}, {Engine}.ComparisonOperator.{Operator(comparison.Operator)}, {Literal(literal)})";
    }

    static string Operator(SemanticComparisonOperator @operator) => @operator switch
    {
        SemanticComparisonOperator.Equal => "Equal",
        SemanticComparisonOperator.NotEqual => "NotEqual",
        SemanticComparisonOperator.GreaterThan => "GreaterThan",
        SemanticComparisonOperator.GreaterThanOrEqual => "GreaterThanOrEqual",
        SemanticComparisonOperator.LessThan => "LessThan",
        SemanticComparisonOperator.LessThanOrEqual => "LessThanOrEqual",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticComparisonOperator), @operator.ToString())
    };

    static string Literal(SemanticValue value) => value switch
    {
        SemanticTextValue text => $"(object){CSharpCodeBuilder.StringLiteral(text.Value)}",
        SemanticNumberValue number => $"(object){number.Value.ToString(CultureInfo.InvariantCulture)}m",
        SemanticBooleanValue boolean => $"(object){(boolean.Value ? "true" : "false")}",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticValue), value.Kind.ToString())
    };

    static void Refusal(SemanticAvailabilityRequirement requirement, string owner, SemanticApplicationContext context, List<ArtifactRenderDiagnostic> diagnostics)
    {
        switch (requirement)
        {
            case SemanticConditionRequirement condition:
                Refusal(condition.Condition, owner, context, diagnostics);
                break;
            case SemanticLogicalAvailability logical:
                Refusal(logical.Left, owner, context, diagnostics);
                Refusal(logical.Right, owner, context, diagnostics);
                break;
        }
    }

    static void Refusal(SemanticAvailabilityCondition condition, string owner, SemanticApplicationContext context, List<ArtifactRenderDiagnostic> diagnostics)
    {
        switch (condition)
        {
            case SemanticLogicalAvailabilityCondition logical:
                Refusal(logical.Left, owner, context, diagnostics);
                Refusal(logical.Right, owner, context, diagnostics);
                break;
            case SemanticAvailabilityComparison comparison:
                if (comparison.Operator is SemanticComparisonOperator.Contains or SemanticComparisonOperator.StartsWith)
                {
                    diagnostics.Add(Error(UnsupportedCondition, $"{owner} compares '$context.{comparison.Path}' with '{comparison.Operator}', which the availability engine does not evaluate.", context));
                }
                else if (comparison.Operand.Path is not null)
                {
                    diagnostics.Add(Error(UnsupportedCondition, $"{owner} compares '$context.{comparison.Path}' with another context value; the availability engine compares a context value with a literal.", context));
                }
                else if (comparison.Operand.Value is not (SemanticTextValue or SemanticNumberValue or SemanticBooleanValue))
                {
                    diagnostics.Add(Error(UnsupportedCondition, $"{owner} compares '$context.{comparison.Path}' with a value that is not text, a number or a boolean.", context));
                }

                break;
        }
    }
}
