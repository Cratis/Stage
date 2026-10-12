// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_exact_numbers : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var compiled = invoice_model.Compile(an_exact_number_model.Source);
        _model = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(compiled));
    }

    void Because() => _plan = invoice_model.Plan(_model);

    [Fact] void should_select_language_v11_from_authored_numbers() => _model.LanguageVersion.ShouldEqual(LanguageVersion.V11);
    [Fact] void should_select_semantic_v11_from_authored_numbers() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V11);
    [Fact] void should_admit_the_model() => Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    [Fact] void should_emit_the_positive_bound() => Text("/when_exact_values.cs").ShouldContain("9007199254740991L");
    [Fact] void should_emit_the_negative_bound() => Text("/when_exact_values.cs").ShouldContain("-9007199254740991L");
    [Fact] void should_emit_the_sixteen_digit_integer() => Text("/when_exact_values.cs").ShouldContain("1234567890123456L");
    [Fact] void should_emit_the_seventeen_digit_fraction() => Text("/when_exact_values.cs").ShouldContain("0.10000000000000002m");
    [Fact] void should_preserve_concept_validation_constants() => Text("/Whole.cs").ShouldContain("<= 9007199254740991m");
    [Fact] void should_preserve_command_validation_constants() => Text("/Record.cs").ShouldContain(">= 0.10000000000000002m");
    [Fact] void should_preserve_requirement_constants() => Text("/Record.cs").ShouldContain("== 1234567890123456m");
    [Fact] void should_preserve_projection_whole_constants() => Text("/Lookup.cs").ShouldContain("1234567890123456L");
    [Fact] void should_preserve_projection_fraction_constants() => Text("/Lookup.cs").ShouldContain("0.10000000000000002m");

    string Text(string suffix) => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith(suffix, StringComparison.Ordinal)).Bytes.AsSpan());
}
