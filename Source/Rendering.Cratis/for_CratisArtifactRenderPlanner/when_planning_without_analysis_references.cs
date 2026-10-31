// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_without_analysis_references : Specification
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_refuse_with_an_integrity_diagnostic_instead_of_throwing(bool validation)
    {
        var loaded = validation
            ? validation_body_model.Load(validation_body_model.Concept("return context.Value.Value > 0;"))
            : generation_rule_model.Load();
        ArtifactRenderPlan? plan = null;
        var previous = PureTransitionAdmission.ReferenceDirectoryOverride.Value;
        PureTransitionAdmission.ReferenceDirectoryOverride.Value = Path.Combine(Path.GetTempPath(), $"absent-stage-ref-{Guid.NewGuid():N}");
        try
        {
            var exception = Catch.Exception(() => plan = opaque_policy_model.Plan(loaded));
            exception.ShouldBeNull();
            plan.ShouldNotBeNull();
            plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
            plan.Artifacts.ShouldBeEmpty();
            plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-021" &&
                diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error &&
                diagnostic.Message.Contains("Reference assemblies 10.0.12", StringComparison.Ordinal));
        }
        finally
        {
            PureTransitionAdmission.ReferenceDirectoryOverride.Value = previous;
        }
    }
}
#endif
