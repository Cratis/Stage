// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.when_rendering_performed_queries;

public class with_projected_performer_queries : given.a_live_performed_query_application
{
    Task Establish() => PrepareLive();

    void Because()
    {
        Evaluate();
        RenderLive();
    }

    [Fact] void should_admit_the_projected_read_model() => _diagnostics.Where(diagnostic => diagnostic.Severity == Contracts.Rendering.ArtifactRenderDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_render_the_live_performer_over_live_read_models() => _source.ShouldContain("LiveReadModels<PersonSummary>");
    [Fact] void should_observe_the_read_model() => _source.ShouldContain("Observe<PersonSummary>(");
    [Fact] void should_not_leave_a_live_gap() => _source.ShouldNotContain("throw new NotImplementedException(");
    [Fact] void should_import_reactive_operators() => _source.ShouldContain("using System.Reactive.Linq;");
}
