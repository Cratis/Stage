// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_production_reactions : Semantics.for_SemanticAutomationArtifactRenderer.given.an_automation
{
    void Establish() => SetContext(Compile("module Work\n  feature Processing\n    slice Automation FollowUp\n      event Completed\n      reaction Worker\n        when Startup\n          produces Completed\n            for \"work\"\n"));

    void Because() => Plan();

    [Fact] void should_refuse_the_application() => _plan.Success.ShouldBeFalse();
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_keep_the_production_error() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-024" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_not_omit_production_contracts() => _plan.Refusals.ShouldBeEmpty();
}
