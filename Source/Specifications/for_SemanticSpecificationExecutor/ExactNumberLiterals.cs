// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

/// <summary>
/// Restores the exact numbers of a specification after Screenplay's source parser has read each literal as a double.
/// The model carries the exact values this way, so a spec can tell a decimal from its neighbor in the last digit.
/// </summary>
static class ExactNumberLiterals
{
    internal static SemanticExecutionPlan Plan(ExecutableSemanticModel model, decimal given, decimal expected)
    {
        var application = model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Specifications = [.. slice.Specifications.Select(specification => specification with
                        {
                            GivenEvents = [.. specification.GivenEvents.Select(fact => fact with { Values = Replace(fact.Values, given) })],
                            ThenReadModels = [.. specification.ThenReadModels.Select(state => state with { Values = Replace(state.Values, expected) })]
                        })]
                    })]
                })]
            })]
        };
        return SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
    }

    static ImmutableArray<SemanticPropertyValue> Replace(ImmutableArray<SemanticPropertyValue> values, decimal exact) =>
        [.. values.Select(value => value with { Value = Replace(value.Value, exact) })];

    static decimal AsParsed(decimal exact) => decimal.Parse(((double)exact).ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);

    static SemanticValue Replace(SemanticValue value, decimal exact) => value switch
    {
        SemanticNumberValue number when number.Value == AsParsed(exact) => SemanticValue.Number(exact),
        SemanticArrayValue array => SemanticValue.Array([.. array.Values.Select(item => Replace(item, exact))]),
        SemanticCompositeValue composite => SemanticValue.Composite([.. composite.Properties.Select(item => item with { Value = Replace(item.Value, exact) })]),
        _ => value
    };
}
