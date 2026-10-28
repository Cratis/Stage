// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_readable_inline_names;

public class with_a_generation_rule : Specification
{
    ArtifactRenderPlan _plan = null!;
    string _concept = null!;

    void Because()
    {
        _plan = opaque_policy_model.Plan(generation_rule_model.Load());
        _concept = _plan.Success ? Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("TenantOrderId.cs", StringComparison.Ordinal)).Bytes.AsSpan()) : string.Empty;
    }

    [Fact] void should_admit_the_verified_body() => Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
    [Fact] void should_name_the_body_after_its_concept() => _concept.ShouldContain("static string GenerateTenantOrderId(");
    [Fact] void should_call_the_same_allocated_method() => _concept.ShouldContain("new(GenerateTenantOrderId(context))");
    [Fact] void should_preserve_the_body() => _concept.ShouldContain(generation_rule_model.PortableBody);
    [Fact] void should_compile_the_generated_application() => RenderedOutput.Errors(opaque_policy_model.Files(_plan)).ShouldBeEmpty();
}
#endif
