// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_concept_date_comparisons : Specification
{
    const string Source = """
        concept Day : Date
        concept Moment : DateTime
        module Scheduling
          feature Windows
            slice StateChange Book
              command Book
                id String identifier
                day Day optional
                at Moment
                validate
                  day > "2026-01-01" message "Too early."
                  at >= "2026-01-01T00:00:00Z" message "Too early."
                produces Booked
                  for id
                  at = at
                returns at
              event Booked
                at Moment
        """;

    ArtifactRenderPlan _plan;
    string _code;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Dates"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Dates", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var model = compilation.Value!.Model;
        _plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model, []).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Dates", "Dates"));
        _code = Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Windows/Book/Book.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    }

    [Fact] void should_render_without_refusals() => _plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_select_the_optional_date_concept_explicitly_in_its_rule() => _code.ShouldContain("RuleFor<Day?>(_ => _.Day).Must(value => value is null || value.Value.CompareTo(new DateOnly(2026, 1, 1)) > 0)");
    [Fact] void should_convert_the_raw_bound_response_back_to_its_concept() => _code.ShouldContain("((Moment)At)");
}
