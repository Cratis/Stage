// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer;

public class when_rendering_protected_read_race_vectors : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _code = null!;

    void Because()
    {
        var model = SemanticModelSerializer.Deserialize(ProtectedCommandReadRaceVectors.Model.EsmBytes.AsSpan());
        _plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("ProtectedCommandReadRaces", "ProtectedCommandReadRaces"));
        _code = string.Join('\n', _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
    }

    [Fact] void should_admit_the_race_vector_model() => _plan.Success.ShouldBeTrue();
    [Fact] void should_have_no_refusals() => _plan.Diagnostics.ShouldBeEmpty();
    [Fact] void should_emit_a_protected_read() => _code.ShouldContain("[global::Cratis.Arc.Commands.LookUpByAttribute(nameof(Update.Lookup))]");
    [Fact] void should_emit_the_append_time_constraint() => _code.ShouldContain("unique.WithName(\"UniqueValue\")");
    [Fact] void should_preserve_the_requirement_message() => _code.ShouldContain("ValidationResult.Error(\"Value is empty.\")");
    [Fact] void should_not_validate_a_read_value_as_a_command_property() => _code.ShouldNotContain("class UpdateValidator");
}
