// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_every_literals_with_joins : Specification
{
    string _code = null!;

    void Because()
    {
        var plan = Plan(every_literal_join_projection.Source);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        _code = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == "Projects/Registration/CompanyLookup/CompanyLookup.cs").Bytes.AsSpan());
    }

    [Fact] void should_emit_native_every_constants() => _code.ShouldContain("every.Set(model => model.Label).ToValue(\"fixed\")");
    [Fact] void should_keep_the_from_mapping_that_declares_join_backfill() => _code.ShouldContain("from.Set(model => model.Company).To(evt => evt.ProjectId)");
    [Fact] void should_keep_the_nonidentifier_join_target() => _code.ShouldContain("join.On(model => model.Company)");
    [Fact] void should_set_the_same_target_natively_as_a_concept() => _code.ShouldContain($"every.Set(model => model.Company).ToValue(new ProjectId(Guid.Parse(\"{every_literal_join_projection.Company}\")))");
    [Fact] void should_not_lower_the_literal_onto_from() => _code.ShouldNotContain("from.Set(model => model.Company).ToValue(");
    [Fact] void should_not_lower_the_label_onto_from() => _code.ShouldNotContain("from.Set(model => model.Label)");
    [Fact] void should_render_a_non_null_numeric_constant() => _code.ShouldContain("every.Set(model => model.Score).ToValue(12.5m)");
    [Fact] void should_leave_child_projections_excluded() => _code.ShouldContain("every.ExcludeChildProjections()");

    [Theory]
    [InlineData("score = null")]
    [InlineData("clear score")]
    public void should_keep_clear_refused(string mapping)
    {
        const string original = "score = 12.5";
        var source = every_literal_join_projection.Source;
        var index = source.LastIndexOf(original, StringComparison.Ordinal);
        var plan = Plan(source[..index] + mapping + source[(index + original.Length)..]);
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-017" &&
            diagnostic.Message.Contains("Every mappings need a Chronicle fluent equivalent", StringComparison.Ordinal));
    }

    static ArtifactRenderPlan Plan(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("every-join"), "every-join", "EveryJoin.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        return CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
    }
}
#endif
