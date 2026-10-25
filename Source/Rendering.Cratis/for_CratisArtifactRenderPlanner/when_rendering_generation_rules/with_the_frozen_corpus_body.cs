// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_generation_rules;

public class with_the_frozen_corpus_body : Specification
{
    ArtifactRenderPlan _plan = null!;
    LoadedSemanticModel _loaded = null!;

    void Establish() => _loaded = generation_rule_corpus.Compile("return value;");
    void Because() => _plan = opaque_policy_model.Plan(_loaded);

    [Fact] void should_keep_artifacts_with_an_explicit_gap() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_compile_the_partial_output() => opaque_policy_model.Compile(_plan).GetTypes().Length.ShouldBeGreaterThan(0);
    [Fact]
    void should_locate_the_unbound_value_at_the_fence()
    {
        var requirement = _loaded.ImplementationRequirements.Single();
        Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" &&
            diagnostic.Message.Contains("CS0103", StringComparison.Ordinal) &&
            diagnostic.Message.Contains($"{requirement.Source.Span.Document}:{requirement.BodyLines[0].Line}:", StringComparison.Ordinal));
    }
}
#endif
