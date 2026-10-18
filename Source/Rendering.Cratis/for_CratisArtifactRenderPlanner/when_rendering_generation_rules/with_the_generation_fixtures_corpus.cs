// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_generation_rules;

public class with_the_generation_fixtures_corpus : Specification
{
    const string Body = "return \"RULE-123\";";
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = opaque_policy_model.Plan(generation_rule_corpus.Compile(Body, ruleOnly: true));

    [Fact] void should_render_the_reachable_rule_factory() => Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
    [Fact] void should_emit_every_line_of_the_resolved_body() => Body.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ShouldEachConformTo(line => _plan.Artifacts.Any(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains(line, StringComparison.Ordinal)));
    [Fact] void should_compile_the_rendered_factory() => RenderedOutput.Errors(opaque_policy_model.Files(_plan)).ShouldBeEmpty();
}
#endif
