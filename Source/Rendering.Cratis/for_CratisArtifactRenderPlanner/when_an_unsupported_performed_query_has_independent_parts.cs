// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_an_unsupported_performed_query_has_independent_parts : Specification
{
    LoadedSemanticModel _loaded = null!;
    SemanticId _owner;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        _loaded = opaque_policy_model.Load(
            """
            module Billing
              feature Invoices
                slice StateView Ledger
                  readmodel Ledger
                    book String key
                    page Int key
                    total Int
                  query LedgerPage => Ledger optional
                    by
                      book String
                      page Int
                    performer
                      ```csharp
                      return readModels.FirstOrDefault();
                      ```
            """ + a_render_with_an_invalid_typed_context.Independent);
        _owner = _loaded.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.ReadModels).Single().Id;
    }

    void Because() => _plan = opaque_policy_model.Plan(_loaded);

    [Fact] void should_preserve_the_owner_diagnostic() => _plan.Diagnostics.Any(diagnostic => string.Equals(diagnostic.Code, "STAGE-ESM-007", StringComparison.Ordinal) && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning && diagnostic.Artifact == _owner && diagnostic.Message.Contains("composite 'by' key", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_record_a_rendering_gap() => _plan.ImplementationGaps.Any(gap => gap.Kind == ArtifactImplementationGapKind.Rendering && string.Equals(gap.Owner, _owner.ToString(), StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_omit_the_unrenderable_owner() => _plan.Artifacts.Any(artifact => artifact.Sources.Contains(_owner)).ShouldBeFalse();
    [Fact] void should_keep_the_independent_command() => _plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/Ping.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_mark_the_output_as_not_runnable() => _plan.Artifacts.Any(artifact => string.Equals(artifact.RelativePath, "STAGE-NOT-RUNNABLE.md", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_be_partial() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_compile_the_remaining_output() => opaque_policy_model.Compile(_plan).GetTypes().Any(type => string.Equals(type.Name, "Ping", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif
