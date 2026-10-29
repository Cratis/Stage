// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_system_identity;

public class with_only_produced_events : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    string _source = string.Empty;
    bool _hasInferredIdentity;

    void Because()
    {
        var reaction = _located.Slice.Reactions.Single(candidate => candidate.Name == "Acknowledge");
        _source = SemanticReactorArtifactRenderer.Render(_located, reaction, _context).Content;
        Plan();
        _hasInferredIdentity = _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-055" && diagnostic.Artifact == reaction.Id);
    }

    [Fact] void should_plan_successfully() => _plan.Success.ShouldBeTrue();
    [Fact] void should_render_the_produced_event() => _source.ShouldContain("Acknowledged");
    [Fact] void should_not_infer_a_system_principal() => _source.ShouldNotContain("ExecuteCommandsAsSystem");
    [Fact] void should_not_report_an_inferred_identity() => _hasInferredIdentity.ShouldBeFalse();
}
