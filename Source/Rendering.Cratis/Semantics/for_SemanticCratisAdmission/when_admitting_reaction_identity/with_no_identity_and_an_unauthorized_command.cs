// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_reaction_identity;

public class with_no_identity_and_an_unauthorized_command : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    string _reactor = string.Empty;

    void Because()
    {
        Plan();
        var reaction = _located.Slice.Reactions.Single(candidate => candidate.Name == "CompletePositive");
        _reactor = SemanticReactorArtifactRenderer.Render(_located, reaction, _context).Content;
    }

    [Fact] void should_report_the_default_system_identity() => _plan.Diagnostics.ShouldContain(diagnostic =>
        diagnostic.Code == "STAGE-ESM-052" && diagnostic.Severity == global::Cratis.Stage.Contracts.Rendering.ArtifactRenderDiagnosticSeverity.Information);
    [Fact] void should_not_report_it_for_a_reaction_that_invokes_nothing() => _plan.Diagnostics.Count(diagnostic => diagnostic.Code == "STAGE-ESM-052").ShouldEqual(1);
    [Fact] void should_report_the_authenticated_default_of_the_command() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-051");
    [Fact] void should_render_the_reactor_with_the_system_identity() => _reactor.ShouldContain("[global::Cratis.Arc.Chronicle.Reactors.ExecuteCommandsAsSystemAttribute]");
    [Fact] void should_plan_the_reactor() => _plan.Artifacts.ShouldNotBeEmpty();
}
