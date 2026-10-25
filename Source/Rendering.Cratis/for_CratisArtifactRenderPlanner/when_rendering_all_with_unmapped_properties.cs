// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// The reference leaves properties that all does not map unset (null) on instances it creates for unrelated events,
/// while Chronicle materializes a default for value types. Only properties Chronicle also leaves null are admitted.
/// </summary>
public class when_rendering_all_with_unmapped_properties : Specification
{
    [Theory]
    [InlineData("count Int")]
    [InlineData("flag Bool")]
    [InlineData("reference Uuid")]
    [InlineData("amount Decimal")]
    [InlineData("tags String[]")]
    void should_refuse_a_property_chronicle_would_default(string property)
    {
        var plan = Plan(property);
        rendering_refusals.Rejected(plan, "STAGE-ESM-017");
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-017" &&
            diagnostic.Message.Contains("neither mapped by all nor optional", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("note String optional")]
    [InlineData("count Int optional")]
    [InlineData("reference Uuid optional")]
    void should_admit_a_property_chronicle_leaves_null(string property)
    {
        var plan = Plan(property);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
    }

    static ArtifactRenderPlan Plan(string property)
    {
        var source = all_events_projection.Source.Replace(
            "      query AllById",
            $"        {property}\n      query AllById",
            StringComparison.Ordinal);
        Assert.NotEqual(all_events_projection.Source, source);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("all"), "all", "All.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join("; ", execution.Issues));

        return CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
    }
}
#endif
