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

/// <summary>A policy named after an audited framework type must not rebind that type inside its own authored body.</summary>
public class with_a_policy_named_math : Specification
{
    const string Body = "return context.Identity.IsAuthenticated && Math.Abs(-1) == 1;";
    ArtifactRenderPlan _plan = null!;
    string _bodies = null!;

    void Because()
    {
        _plan = opaque_policy_model.Plan(opaque_policy_model.Source(Body, "Math", "Staff", "Math"));
        Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
        opaque_policy_model.Compile(_plan);
        _bodies = string.Concat(_plan.Artifacts.Where(artifact => artifact.RelativePath.StartsWith("GeneratedPolicies/PolicyBodies", StringComparison.Ordinal))
            .Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
    }

    [Fact] void should_render_admitted_code() => _plan.Success.ShouldBeTrue();
    [Fact] void should_keep_the_authored_body() => _bodies.ShouldContain("Math.Abs(-1) == 1");
    [Fact] void should_not_name_the_body_method_after_the_framework_type() => _bodies.ShouldNotContain("static bool Math(");
}
#endif
