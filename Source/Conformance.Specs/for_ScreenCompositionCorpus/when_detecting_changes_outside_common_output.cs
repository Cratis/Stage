// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ScreenCompositionCorpus;

public class when_detecting_changes_outside_common_output : given.the_planned_screen_composition_corpus
{
    [Theory]
    [InlineData("Program.cs")]
    [InlineData("src/stage.ts")]
    [InlineData(".editorconfig")]
    public void should_not_hide_an_unrelated_change_in_the_qualified_oracle(string path)
    {
        var form = ScreenCompositionCorpus.V1.SourceForms.Single(form => form.Name == "single");
        var planned = Plan(form.Documents, form.IdentityCatalogBytes.AsSpan(), carryScene: false);
        var options = new CratisRenderingOptions(ApplicationName, ApplicationName);
        var request = new ArtifactRenderRequest(planned.Model, SemanticExecutionPlan.Compile(planned.Model).Plan!, CratisRendering.CreateProfile(ApplicationName, options), new(ArtifactRenderScopeKind.Application, planned.Model.Application.Id));
        var changed = ArtifactRenderPlan.Create(
            request,
            [.. planned.Plan.Artifacts.Select(artifact => artifact.RelativePath == path
                ? PlannedArtifact.CreateText(path, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()) + "\n// unrelated change\n", artifact.Sources)
                : artifact)],
            planned.Plan.Diagnostics);
        QualifiedCommonOutput.Digest(planned.Model, changed, options).ShouldNotEqual("0f8dc52e3c77d034e0ce0f4c317bd699cf121a8484c0b17f911e87206ef05c3e");
    }
}
