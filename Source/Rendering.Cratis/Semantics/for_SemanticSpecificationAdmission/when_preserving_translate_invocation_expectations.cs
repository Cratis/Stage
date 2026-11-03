// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission;

public class when_preserving_translate_invocation_expectations : a_translation_specification_application
{
    SemanticId _specification;

    void Establish()
    {
        var command = _model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Commands).First();
        var application = _model.Application with
        {
            Modules = [.. _model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice.Name != "TrackShipments" ? slice : slice with
                    {
                        Specifications = [.. slice.Specifications.Select(specification => specification with { ThenInvocations = [new(command.Id, [])] })]
                    })]
                })]
            })]
        };
        _model = ExecutableSemanticModel.Create(LanguageVersion.V34, SemanticVersion.V34, application);
        _specification = _model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "TrackShipments").Specifications.Single().Id;
    }

    void Because() => _plan = Plan(_model);

    [Fact] void should_keep_the_invocation_preservation_refusal_blocking() => Assert.False(_plan.Success);
    [Fact] void should_refuse_the_original_translate_specification() => Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Artifact == _specification && diagnostic.Code == "STAGE-ESM-016" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_not_silently_render_the_translate_specification() => Assert.DoesNotContain(_plan.Artifacts, artifact => artifact.RelativePath.Contains("when_receiving_a_shipment", StringComparison.Ordinal));
}
#endif
