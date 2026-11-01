// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_SemanticPolicyArtifactRenderer;

public class when_rendering_a_datetimeoffset_cutoff_policy : Specification
{
    const string Body = "return context.Occurred < new DateTimeOffset(2026, 11, 30, 0, 0, 0, TimeSpan.Zero);";
    ArtifactRenderPlan _plan = null!;
    ExecutableSemanticModel _model = null!;
    Assembly _assembly = null!;

    void Because()
    {
        var loaded = opaque_policy_model.Load(opaque_policy_model.Source(Body));
        _model = loaded.Model;
        _plan = opaque_policy_model.Plan(loaded);
        Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
        _assembly = opaque_policy_model.Compile(_plan);
    }

    [Fact] void should_render_the_policy_successfully() => _plan.Success.ShouldBeTrue();
    [Fact] void should_allow_a_command_before_the_cutoff() => opaque_policy_model.AllowsCommand(_assembly, _model, opaque_policy_model.Caller(), new(2026, 11, 29, 23, 59, 59, TimeSpan.Zero)).ShouldBeTrue();
    [Fact] void should_deny_a_command_at_the_cutoff() => opaque_policy_model.AllowsCommand(_assembly, _model, opaque_policy_model.Caller(), new(2026, 11, 30, 0, 0, 0, TimeSpan.Zero)).ShouldBeFalse();
    [Fact] void should_deny_a_command_after_the_cutoff() => opaque_policy_model.AllowsCommand(_assembly, _model, opaque_policy_model.Caller(), new(2026, 12, 1, 0, 0, 0, TimeSpan.Zero)).ShouldBeFalse();
}
#endif
