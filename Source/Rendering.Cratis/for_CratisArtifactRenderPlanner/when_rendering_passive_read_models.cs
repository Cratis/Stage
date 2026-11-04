// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_passive_read_models : Specification
{
    internal const string Source = """
        command reads protected
        module Stock
          feature Decisions
            slice StateChange Adjust
              readmodel Balance
                passive
                id String key
                quantity Int
              projection BuildBalance => Balance
                from Adjusted
                  id = $eventSourceId
                  quantity = quantity
                remove with Removed
              event Adjusted
                quantity Int
              event Removed
              command Adjust
                id String identifier
                quantity Int
                reads Balance by id
                produces Adjusted
                  for id
                  quantity = quantity
            slice StateView Listing
              readmodel ListingBalance
                id String key
                quantity Int
              projection BuildListing => ListingBalance
                from Adjusted
                  id = $eventSourceId
                  quantity = quantity
              query AllBalances => ListingBalance[]
        """;

    SemanticCompilation _compilation = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish() => _compilation = Compile(Source);
    void Because() => _plan = Plan(_compilation);

    [Fact] void should_render_successfully() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
    [Fact] void should_mark_only_the_explicit_passive_model() => Text(_plan, "Adjust.cs").ShouldContain("[Passive]");
    [Fact] void should_mark_the_native_projection_passive() => Text(_plan, "Adjust.cs").ShouldContain("builder.Passive();");
    [Fact] void should_keep_the_same_shaped_sibling_active() => Text(_plan, "Listing.cs").ShouldNotContain("Passive");
    [Fact] void should_not_emit_a_query_on_the_passive_model() => Text(_plan, "Adjust.cs").ShouldNotContain("GetAll");
    [Fact] void should_keep_the_active_siblings_query() => Text(_plan, "Listing.cs").ShouldContain("AllBalances");
    [Fact] void should_keep_the_passive_model_available_to_command_reads() => Text(_plan, "Adjust.cs").ShouldContain("Balance? balance");
    [Fact] void should_emit_the_same_bytes_twice() => Assert.Equal(_plan.Artifacts.Select(artifact => artifact.Sha256), Plan(_compilation).Artifacts.Select(artifact => artifact.Sha256));

    [Fact]
    void should_change_the_digest_without_changing_portable_semantics()
    {
        var active = Compile(Source.Replace("        passive\n", string.Empty, StringComparison.Ordinal));
        active.Model.Revision.ShouldEqual(_compilation.Model.Revision);
        Assert.Equal(SemanticModelSerializer.Serialize(active.Model), SemanticModelSerializer.Serialize(_compilation.Model));
        Plan(active).Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Adjust.cs", StringComparison.Ordinal)).Sha256
            .ShouldNotEqual(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Adjust.cs", StringComparison.Ordinal)).Sha256);
    }

    [Fact]
    void should_change_only_the_explicit_passive_output()
    {
        var active = Plan(Compile(Source.Replace("        passive\n", string.Empty, StringComparison.Ordinal)));
        Text(_plan, "Adjust.cs").Replace("[Passive]\n", string.Empty, StringComparison.Ordinal)
            .Replace("        builder.Passive();\n", string.Empty, StringComparison.Ordinal).ShouldEqual(Text(active, "Adjust.cs"));
        Text(_plan, "Listing.cs").ShouldEqual(Text(active, "Listing.cs"));
    }

    [Fact]
    void should_preserve_feature_scoped_passivity()
    {
        var model = _compilation.Model;
        var execution = SemanticExecutionPlan.Compile(model, []).Plan!;
        var plan = new CratisArtifactRenderPlanner().Plan(new(model, execution, CratisRendering.WithPassiveReadModels(Profile(_compilation), _compilation), new(ArtifactRenderScopeKind.Feature, model.Application.Modules.Single().Features.Single().Id)));
        Assert.True(plan.Success, string.Join("; ", plan.Diagnostics));
        Text(plan, "Adjust.cs").ShouldContain("[Passive]");
        Text(plan, "Listing.cs").ShouldNotContain("Passive");
    }

    [Fact]
    void should_refuse_screen_data_observing_a_passive_model()
    {
        var source = Source.Replace("      event Adjusted", "      query Hidden => Balance[]\n      screen List\n        data Balance[] via query Hidden\n      event Adjusted", StringComparison.Ordinal);
        var result = CompileResult(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Count(diagnostic => diagnostic.Code == "PLAY0859").ShouldEqual(2);
    }

    [Fact]
    void should_preserve_non_passive_profile_bytes()
    {
        var active = Compile(Source.Replace("        passive\n", string.Empty, StringComparison.Ordinal));
        var profile = Profile(active);
        Assert.Equal(profile.Inputs, CratisRendering.WithPassiveReadModels(profile, active).Inputs);
    }

    [Theory]
    [InlineData("Balance")]
    [InlineData("Balance[]")]
    [InlineData("Balance optional")]
    [InlineData("observable Balance[]")]
    public void should_keep_passive_queries_refused_by_screenplay(string shape)
    {
        var source = Source.Replace("      event Adjusted", $"      query FindPassive => {shape}\n      event Adjusted", StringComparison.Ordinal);
        var result = CompileResult(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("PLAY0859");
    }

    [Fact]
    void should_render_model_bound_passive_projections()
    {
        var compilation = Compile(Source.Replace("        remove with Removed\n", string.Empty, StringComparison.Ordinal).Replace("        reads Balance by id\n", string.Empty, StringComparison.Ordinal));
        var profile = CratisRendering.WithPassiveReadModels(Profile(compilation), compilation);
        var convention = ReducerConventionInput.Read("Projects", """{"version":3,"application":"Projects","reducerConvention":"classic","projectionConvention":"model-bound"}""");
        var plan = Plan(compilation, ReplaceInputs(profile, [.. profile.Inputs, convention]));
        Assert.True(plan.Success, string.Join("; ", plan.Diagnostics));
        Text(plan, "Adjust.cs").ShouldContain("[Passive]");
        Text(plan, "Adjust.cs").ShouldContain("[FromEvent");
    }

    internal static SemanticCompilation Compile(string source)
    {
        var result = CompileResult(source);
        Assert.True(result.Success, string.Join("; ", result.Diagnostics));
        return result.Value!;
    }

    static global::Cratis.Screenplay.CompilationResult<SemanticCompilation> CompileResult(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("passive"), "passive", "Stock.play", source);
        return new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
    }

    internal static ArtifactRenderProfile Profile(SemanticCompilation compilation) => CratisRendering.CreateProfile(compilation.Model.Application.Name, new("Projects", "Projects"));
    internal static ArtifactRenderProfile ReplaceInputs(ArtifactRenderProfile profile, IEnumerable<ArtifactRenderInput> inputs) => ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. inputs]);
    internal static ArtifactRenderPlan Plan(SemanticCompilation compilation, ArtifactRenderProfile? profile = null) => new CratisArtifactRenderPlanner().Plan(new(compilation.Model, SemanticExecutionPlan.Compile(compilation.Model, []).Plan!, profile ?? CratisRendering.WithPassiveReadModels(Profile(compilation), compilation), new(ArtifactRenderScopeKind.Application, compilation.Model.Application.Id)));
    internal static string Text(ArtifactRenderPlan plan, string name) => Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith(name, StringComparison.Ordinal)).Bytes.AsSpan());
}
#endif
