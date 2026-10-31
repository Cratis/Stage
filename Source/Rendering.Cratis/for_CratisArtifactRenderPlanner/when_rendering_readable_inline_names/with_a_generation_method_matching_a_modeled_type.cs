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

public class with_a_generation_method_matching_a_modeled_type : Specification
{
    const string Body = "return \"tenant:fixture\";";
    ArtifactRenderPlan _plan = null!;
    string _concept = null!;

    void Because()
    {
        var source = "concept GenerateTenantOrderId : Enum\n  accepted\n" + generation_rule_model.Source(Body);
        _plan = opaque_policy_model.Plan(opaque_policy_model.Load(source));
        _concept = _plan.Success ? Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/TenantOrderId.cs", StringComparison.Ordinal)).Bytes.AsSpan()) : string.Empty;
    }

    [Fact] void should_admit_the_authored_body() => Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
    [Fact] void should_suffix_the_method() => _concept.ShouldContain("static string GenerateTenantOrderId_");
    [Fact] void should_call_the_suffixed_method() => _concept.ShouldContain("new(GenerateTenantOrderId_");
    [Fact] void should_preserve_the_authored_body() => _concept.ShouldContain(Body);
    [Fact] void should_compile_the_generated_application() => RenderedOutput.Errors(opaque_policy_model.Files(_plan)).ShouldBeEmpty();
}
#endif
