// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_system_identity;

public class with_an_anonymous_command : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    string _source = string.Empty;

    void Because()
    {
        SetContext(Compile("policy Anyone\n  require anonymous\n" + Source.Replace("command CompleteWork", "command CompleteWork\n        authorize Anyone", StringComparison.Ordinal)));
        var reaction = _located.Slice.Reactions.Single(candidate => candidate.Name == "CompletePositive");
        _source = SemanticReactorArtifactRenderer.Render(_located, reaction, _context).Content;
        Plan();
    }

    [Fact] void should_plan_successfully() => _plan.Success.ShouldBeTrue();
    [Fact] void should_not_infer_a_system_identity() => _source.ShouldNotContain("ExecuteCommandsAsSystem");
    [Fact] void should_not_report_an_inferred_identity() => _plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-055");
    [Fact] void should_report_the_explicit_anonymous_policy() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-060");
}
