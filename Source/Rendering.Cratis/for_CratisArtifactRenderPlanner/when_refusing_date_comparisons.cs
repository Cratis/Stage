// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_date_comparisons : Specification
{
    [Theory]
    [InlineData("concept Moment : DateTime pii\n", "Moment", "at", "carries compliance")]
    [InlineData("", "DateTime", "from_date", "camel case")]
    [InlineData("concept Moment : DateTime\n", "Moment?", "at", "optional concept 'Moment'")]
    public void should_refuse_a_compared_datetime_property_arc_cannot_bind_exactly(string declarations, string type, string name, string reason)
    {
        var source = $$"""
            {{declarations}}module Scheduling
              feature Windows
                slice StateChange Keep
                  command Keep
                    id String identifier
                    {{name}} {{type}}
                    validate
                      require {{name}} >= "2026-01-01T00:00:00Z"
                    produces Kept
                      for id
                  event Kept
            """;
        var model = Compile(source);
        var diagnostics = Plan(model, new(ArtifactRenderScopeKind.Application, model.Application.Id)).Diagnostics;
        diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-046" && diagnostic.Message.Contains(reason, StringComparison.Ordinal) && diagnostic.Message.Contains($"'{name}'", StringComparison.Ordinal));
    }

    static ExecutableSemanticModel Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Dates"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", source);
        var compilation = new SemanticModelCompiler().Compile("Dates", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));

        return compilation.Value!.Model;
    }

    static ArtifactRenderPlan Plan(ExecutableSemanticModel model, ArtifactRenderScope scope) =>
        CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model, []).Plan!, scope, new("Dates", "Dates"));
}
