// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOwnerScopedDegradation.when_evaluating;

public class with_an_owner_scoped_event : for_CratisArtifactRenderPlanner.when_degrading_an_owner.given.an_unsupported_event_request
{
    SemanticApplicationContext _context = null!;
    SemanticDependencyIndex _index = null!;
    readonly List<ArtifactRenderDiagnostic> _diagnostics = [];
    IReadOnlyDictionary<SemanticId, string>? _roots;

    void Establish()
    {
        _context = new(_request, _options);
        _index = new(_context);
        SemanticCratisAdmission.Evaluate(_context, _context.SelectedSlices(), _diagnostics);
    }

    void Because() => _roots = SemanticOwnerScopedDegradation.TryRoots(_context, _diagnostics, _index, new(false));

    [Fact] void should_attribute_the_error_to_the_event() => _context.OwnerScopedRefusals.Exists(diagnostic => diagnostic.Artifact == _unsupportedEvent).ShouldBeTrue();
    [Fact] void should_qualify_the_event_as_a_root() => _roots![_unsupportedEvent].ShouldEqual("STAGE-DEGRADE-001");
    [Fact] void should_reject_a_read_model_omission_when_migration_removals_are_planned() => SemanticOwnerScopedDegradation.TryRoots(_context, _diagnostics, _index, new(false, [new("unused", "unused", _model.Application.Id)])).ShouldBeNull();
    [Fact] void should_bypass_omission_in_strict_mode() => SemanticOwnerScopedDegradation.TryRoots(_context, _diagnostics, _index, new(true)).ShouldBeNull();
    [Fact] void should_reject_an_unattributed_error_even_on_the_root() => SemanticOwnerScopedDegradation.TryRoots(_context, [.. _diagnostics, new("STAGE-ESM-012", ArtifactRenderDiagnosticSeverity.Error, "An independent event-shape defect.", _unsupportedEvent)], _index, new(false)).ShouldBeNull();
    [Fact] void should_reject_an_unrelated_error() => SemanticOwnerScopedDegradation.TryRoots(_context, [.. _diagnostics, new("STAGE-ESM-006", ArtifactRenderDiagnosticSeverity.Error, "An unrelated command failure.", _independentCommand)], _index, new(false)).ShouldBeNull();
}
