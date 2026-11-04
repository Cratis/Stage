// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_opaque_policies;

/// <summary>A policy whose name is not an audited framework name keeps its readable body method name.</summary>
public class with_a_policy_named_clear_of_audited_names : Specification
{
    const string Body = "return context.Identity.IsAuthenticated && Math.Abs(-1) == 1;";
    ArtifactRenderPlan _plan = null!;
    string _bodies = null!;

    void Because()
    {
        _plan = opaque_policy_model.Plan(opaque_policy_model.Source(Body, "Custom", "Staff", "Custom"));
        Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
        opaque_policy_model.Compile(_plan);
        _bodies = string.Concat(_plan.Artifacts.Where(artifact => artifact.RelativePath.StartsWith("GeneratedPolicies/PolicyBodies", StringComparison.Ordinal))
            .Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
    }

    [Fact] void should_render_admitted_code() => _plan.Success.ShouldBeTrue();
    [Fact] void should_keep_the_authored_body() => _bodies.ShouldContain("Math.Abs(-1) == 1");
    [Fact] void should_keep_the_readable_body_method_name() => _bodies.ShouldContain("static bool Custom(");
}
#endif
