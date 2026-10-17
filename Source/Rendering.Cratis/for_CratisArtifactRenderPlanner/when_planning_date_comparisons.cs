// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_date_comparisons : Specification
{
    const string Marker = "[property: global::Cratis.Arc.Commands.ExplicitOffset] global::System.DateTimeOffset";

    [Theory]
    [InlineData("single")]
    [InlineData("folder")]
    [InlineData("reordered")]
    [InlineData("relocated")]
    public void should_render_the_v26_corpus_in_every_source_form(string formName)
    {
        var plan = Plan(formName, out var model);
        model.SemanticVersion.ShouldEqual(SemanticVersion.V26);
        plan.Success.ShouldBeTrue();
        plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-046");
        Text(plan, "Keep/Keep.cs").ShouldContain("public record KeepWindow(string Id, " + Marker + " From, " + Marker + " To)");
        Text(plan, "Keep/Keep.cs").ShouldContain("(command.To.CompareTo(command.From) >= 0)");
        Text(plan, "Compare/Compare.cs").ShouldContain("public record CompareInstant(string Id, " + Marker + " At)");
        Text(plan, "Compare/Compare.cs").ShouldContain("RuleFor(_ => _.At).Must(value => value.CompareTo(new global::System.DateTimeOffset(639028224000000000L, global::System.TimeSpan.Zero)) == 0)");
        Text(plan, "Calendar/Calendar.cs").ShouldContain("(Day.CompareTo(new global::System.DateOnly(2026, 1, 1)) > 0)");
        Text(plan, "Calendar/Calendar.cs").ShouldNotContain("ExplicitOffset");
    }

    [Fact]
    public void should_render_every_specification_of_the_v26_corpus()
    {
        var plan = Plan("single", out _);
        var specifications = plan.Artifacts.Where(artifact => artifact.RelativePath.Contains("/when_", StringComparison.Ordinal)).ToArray();
        specifications.Length.ShouldEqual(DateComparisonsCorpus.V26.SpecificationExpectations.Length);
    }

    static string Text(ArtifactRenderPlan plan, string suffix) =>
        Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Windows/" + suffix, StringComparison.Ordinal)).Bytes.AsSpan());

    static ArtifactRenderPlan Plan(string formName, out ExecutableSemanticModel model)
    {
        var corpus = DateComparisonsCorpus.V26;
        var form = corpus.SourceForms.Single(form => form.Name == formName);
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        model = compilation.Value!.Model;

        return CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model, []).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Dates", "Dates"));
    }
}
