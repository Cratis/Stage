// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_composite_read_model_keys : Specification
{
    ExecutableSemanticModel _model = null!;
    SemanticSlice _slice = null!;
    ArtifactRenderPlan _application = null!;
    Exception? _error;

    void Establish()
    {
        _model = SemanticModelSerializer.Deserialize(CompositeReadModelKeysCorpus.V27.EsmBytes.AsSpan());
        _slice = SemanticVersionFeatures.Slices(_model.Application).Single();
    }

    void Because() => _error = Catch.Exception(() => _application = Plan(new(ArtifactRenderScopeKind.Application, _model.Application.Id)));

    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_admit_the_version() => _application.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-016");
    [Fact] void should_render_the_composite_keyed_read_model() => _application.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-048");
    [Fact]
    void should_refuse_the_query_by_parts()
    {
        Refuses(_application, "STAGE-ESM-047", _slice.Queries.Single(query => query.Name == "Total").Id, "Total").ShouldBeTrue();
        given.rendering_refusals.Rejected(_application, "STAGE-ESM-047");
    }
    [Fact] void should_not_refuse_the_list_queries() => _application.Diagnostics.Count(diagnostic => diagnostic.Code == "STAGE-ESM-047").ShouldEqual(1);

    static bool Refuses(ArtifactRenderPlan plan, string code, SemanticId artifact, string name) =>
        plan.Diagnostics.Any(diagnostic => diagnostic.Code == code && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning &&
            diagnostic.Artifact == artifact && diagnostic.Message.Contains($"'{name}'", StringComparison.Ordinal));

    ArtifactRenderPlan Plan(ArtifactRenderScope scope) =>
        CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model, []).Plan!, scope, new("Payments", "Payments"));
}
