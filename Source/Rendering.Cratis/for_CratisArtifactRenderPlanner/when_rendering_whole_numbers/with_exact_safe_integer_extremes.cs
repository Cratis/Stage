// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_whole_numbers;

public class with_exact_safe_integer_extremes : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var model = a_whole_number_model.Compile(SemanticVersion.V8);
        var module = model.Application.Modules[0];
        var feature = module.Features[0];
        var slice = feature.Slices[0] with
        {
            Specifications = [.. feature.Slices[0].Specifications.Select(WithExactValues)]
        };
        _model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice, feature.Slices[1]] }] }]
        });
    }

    void Because() => _plan = invoice_model.Plan(_model);

    [Fact] void should_admit_the_executable_model() => _plan.Success.ShouldBeTrue();
    [Fact] void should_keep_the_concept_long() => Text("/Quantity.cs").ShouldContain("Quantity(long Value)");
    [Fact] void should_keep_the_command_long() => Text("/Record.cs").ShouldContain("long Count");
    [Fact] void should_emit_the_exact_positive_extreme() => Text("/when_recording_positive.cs").ShouldContain("9007199254740991L");
    [Fact] void should_emit_the_exact_negative_extreme() => Text("/when_recording_negative.cs").ShouldContain("-9007199254740991L");
    [Fact] void should_project_the_exact_positive_extreme() => Text("/when_recording_positive_is_projected.cs").ShouldContain("9007199254740991L");
    [Fact] void should_project_the_exact_negative_extreme() => Text("/when_recording_negative_is_projected.cs").ShouldContain("-9007199254740991L");

    static SemanticSpecification WithExactValues(SemanticSpecification specification)
    {
        var value = specification.Name == "RecordingPositive" ? 9007199254740991m : -9007199254740991m;
        var action = specification.When!;
        return specification with
        {
            When = action with { Values = ReplaceNumbers(action.Values, value) },
            ThenEvents = [.. specification.ThenEvents.Select(expected => expected with { Values = ReplaceNumbers(expected.Values, value) })],
            ThenReadModels = [.. specification.ThenReadModels.Select(expected => expected with { Values = ReplaceNumbers(expected.Values, value) })]
        };
    }

    static ImmutableArray<SemanticPropertyValue> ReplaceNumbers(ImmutableArray<SemanticPropertyValue> values, decimal number) =>
        [.. values.Select(value => value.Value is SemanticNumberValue ? value with { Value = SemanticValue.Number(number) } : value)];

    string Text(string suffix) => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith(suffix, StringComparison.Ordinal)).Bytes.AsSpan());
}
