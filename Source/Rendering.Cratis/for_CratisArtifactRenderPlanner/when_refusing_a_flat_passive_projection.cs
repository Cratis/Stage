// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_passive_read_models;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_a_flat_passive_projection : Specification
{
    SemanticApplicationContext _context = null!;
    ImmutableArray<ArtifactRenderDiagnostic> _diagnostics;

    void Establish()
    {
        var compilation = Compile(Source.Replace("        reads Balance by id\n", string.Empty, StringComparison.Ordinal));
        var original = compilation.Model;
        var module = original.Application.Modules.Single();
        var feature = module.Features.Single();
        // The compiler emits scoped projections; other portable-model producers can supply a flat one.
        var slices = feature.Slices.Select(slice => slice with
        {
            Projections = [.. slice.Projections.Select(projection => compilation.PassiveReadModels.Contains(projection.ReadModel)
                ? projection with { Scope = null }
                : projection)]
        });
        var model = ExecutableSemanticModel.Create(original.LanguageVersion, original.SemanticVersion, original.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. slices] }] }]
        });
        var execution = SemanticExecutionPlan.Compile(original).Plan!;
        var profile = CratisRendering.WithPassiveReadModels(Profile(compilation), compilation);
        var request = new ArtifactRenderRequest(model, execution, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _context = new(request, new("Projects", "Projects"));
    }

    void Because() => _diagnostics = SemanticCratisAdmission.Evaluate(_context, _context.SelectedSlices());

    [Fact]
    void should_refuse_replay_without_a_scoped_event_source_key() =>
        _diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-061").Message
            .ShouldContain("the root key must be the event-source id");
}
#endif
