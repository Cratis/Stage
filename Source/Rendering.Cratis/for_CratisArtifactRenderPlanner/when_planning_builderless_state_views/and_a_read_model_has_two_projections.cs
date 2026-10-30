// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_builderless_state_views;

public class and_a_read_model_has_two_projections : Specification
{
    ImmutableArray<ArtifactRenderDiagnostic> _diagnostics;

    void Because()
    {
        var model = invoice_model.Compile(builderless_state_view_model.BuiltQuery);
        var options = new CratisRenderingOptions("WelcomeApp", "Welcome");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var located = context.SelectedSlices().Single(slice => slice.Slice.Kind == SemanticSliceKind.StateView);
        var projection = located.Slice.Projections.Single();

        // Screenplay rejects duplicate builders at compile time; exercise Stage admission with the malformed semantic slice directly.
        var slice = located.Slice with { Projections = [projection, projection with { Name = "OtherWelcomeRows" }] };
        _diagnostics = SemanticCratisAdmission.Evaluate(context, [located with { Slice = slice }]);
    }

    [Fact] void should_refuse_the_structural_defect() => _diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-007" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_not_degrade_the_structural_defect() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(ArtifactRenderDegradationCodes.UnsupportedConstruct);
}
