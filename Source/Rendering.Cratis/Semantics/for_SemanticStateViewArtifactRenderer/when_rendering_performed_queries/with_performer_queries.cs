// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer.when_rendering_performed_queries;

public class with_performer_queries : given.a_performed_query_application
{
    void Establish() => Prepare(Source);

    void Because()
    {
        Evaluate();
        Render();
    }

    [Fact] void should_admit_the_read_model_without_a_projection() => _diagnostics.Where(diagnostic => diagnostic.Severity == global::Cratis.Stage.Contracts.Rendering.ArtifactRenderDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_render_the_read_model() => _source.ShouldContain("public record InvoiceFigures(");
    [Fact] void should_render_the_keyed_lookup_with_its_argument() => _source.ShouldContain("Task<InvoiceFigures?> CurrentFigures(global::Cratis.Chronicle.ReadModels.IReadModels __stageReadModelStore, global::Cratis.Arc.Queries.IQueryContextManager __stageQueryContexts, global::Cratis.Arc.Tenancy.ITenantIdAccessor __stageTenantAccessor, global::Cratis.Arc.Authorization.ICurrentPrincipalAccessor __stagePrincipalAccessor, string invoiceId)");
    [Fact] void should_render_the_filtered_list_with_its_filters_in_order() => _source.ShouldContain("string region, string? status = null)");
    [Fact] void should_render_the_observable_list() => _source.ShouldContain("ISubject<global::System.Collections.Generic.IEnumerable<InvoiceFigures>> LiveFigures(");
    [Fact] void should_keep_the_modeled_authorization() => _source.ShouldContain("AuthorizeAttribute");
    [Fact] void should_name_the_performer_each_query_waits_for() => _source.ShouldContain("Queries/CurrentFigures.cs");
    [Fact] void should_not_pretend_the_live_performer_can_answer() => _source.ShouldContain("throw new global::System.NotImplementedException(");
    [Fact] void should_not_project_the_read_model() => _source.ShouldNotContain("IProjectionFor");
}
