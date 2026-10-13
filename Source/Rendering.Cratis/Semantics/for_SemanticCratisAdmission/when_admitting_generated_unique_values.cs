// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_admitting_generated_unique_values : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var model = an_invite_code_model.Model();
        _plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Invites", "Invites"));
    }

    [Fact] void should_admit_the_pinned_constraint_histories() => _plan.Success.ShouldBeTrue();
    [Fact] void should_not_validate_a_generated_parameter_as_a_request_member() => _plan.Artifacts.Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())).Any(text => text.Contains("RuleFor(_ => _.InviteCode)", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_emit_named_append_time_constraints() => _plan.Artifacts.Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())).Any(text => text.Contains("WithName(\"UniqueInviteCode\")", StringComparison.Ordinal)).ShouldBeTrue();

    [Theory]
    [InlineData("uuid")]
    [InlineData("uuidv7")]
    [InlineData("ulid")]
    [InlineData("typeid \"invite\"")]
    [InlineData("nanoid 1 alphabet \"AB\"")]
    [InlineData("nanoid 256 alphabet \"AB\"")]
    [InlineData("bytes 1 hex")]
    [InlineData("bytes 512 base64url")]
    void should_admit_each_string_strategy_and_size_boundary(string strategy)
    {
        var source = an_invite_code_model.Source[..an_invite_code_model.Source.IndexOf("      specification Collision", StringComparison.Ordinal)]
            .Replace("generate nanoid 6 alphabet \"AB\"", "generate " + strategy, StringComparison.Ordinal);
        var model = invoice_model.Compile(source);
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Invites", "Invites"));
        plan.Success.ShouldBeTrue();
    }
}
