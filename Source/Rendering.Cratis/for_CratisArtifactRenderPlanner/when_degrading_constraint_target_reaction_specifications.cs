// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_degrading_constraint_target_reaction_specifications : Semantics.for_SemanticAutomationArtifactRenderer.given.an_automation
{
    void Establish() => SetContext(Compile(Semantics.for_SemanticReactionSpecificationRenderer.given.a_reaction_specification.ProductionSource + "\n      constraint UniqueId\n        unique id on Started\n"));

    void Because() => Plan();

    [Fact] void should_preserve_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_reaction() => _plan.Artifacts.ShouldContain(artifact => artifact.RelativePath.EndsWith("Worker.cs", StringComparison.Ordinal));
    [Fact] void should_omit_the_unrenderable_specification() => _plan.Artifacts.SelectMany(artifact => artifact.Sources).ShouldNotContain(_located.Slice.Specifications.Single().Id);
    [Fact] void should_warn_without_relaxing_the_guard() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning && diagnostic.Message.Contains("Following", StringComparison.Ordinal) && diagnostic.Message.Contains("constraint", StringComparison.Ordinal));
}
