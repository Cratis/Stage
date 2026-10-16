// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_pattern_and_random_generation : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var model = a_pattern_random_model.Model();
        _plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("PatternRandom", "PatternRandom"));
    }

    [Fact] void should_admit_the_strategies() => Assert.True(_plan.Success, string.Join(';', _plan.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_render_a_pure_pattern_factory() => File("Reference").ShouldContain("GenerateValue.Pattern(\"REF-?{16}\")");
    [Fact] void should_render_a_contextual_date_factory() => File("DatedReference").ShouldContain("GenerateValue.Pattern(\"{yyyy}-{MM}-{dd}-?{16}\", context)");
    [Fact] void should_implement_contextual_generation_only_for_dates() => File("DatedReference").ShouldContain("IGeneratableInContext<DatedReference>");
    [Fact] void should_render_the_checksum_argument() => File("CheckedReference").ShouldContain("GenerateValue.Pattern(\"#{16}\", Checksum.Luhn)");
    [Fact] void should_preserve_the_full_admitted_int64_range() => File("Ticket").ShouldContain("GenerateValue.Integer(-9223372036854775808L, 9223372036854775807L)");
    [Fact] void should_not_narrow_v21_whole_numbers_to_int32() => File("Ticket").ShouldContain("Ticket(long Value)");
    [Fact] void should_never_read_the_render_time_clock() => File("DatedReference").ShouldNotContain("UtcNow");
    [Fact] void should_render_identical_bytes_on_repeated_planning()
    {
        var model = a_pattern_random_model.Model();
        var repeated = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("PatternRandom", "PatternRandom"));
        _plan.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)).SequenceEqual(repeated.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256))).ShouldBeTrue();
    }

    string File(string name) => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == $"Common/{name}.cs").Bytes.AsSpan());
}
