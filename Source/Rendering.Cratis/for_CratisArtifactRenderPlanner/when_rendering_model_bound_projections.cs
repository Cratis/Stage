// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Renders From-only scoped projections as model-bound read models for applications selecting the modern conventions.
/// </summary>
public class when_rendering_model_bound_projections
{
    const string File = "Projects/Registration/ProjectLookup/ProjectLookup.cs";

    internal static readonly string Source = when_rendering_scoped_projections.ScopedSource[..when_rendering_scoped_projections.ScopedSource.IndexOf("    slice StateView ProjectLookup", StringComparison.Ordinal)] + """
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
                name ProjectName
              query ProjectById => ProjectSummary?
                by projectId ProjectId
              readmodel ProjectDetails
                projectId ProjectId
                name ProjectName
              query ProjectDetailsById => ProjectDetails?
                by projectId ProjectId
              projection ProjectDetailsProjection => ProjectDetails
                from ProjectRegistered key projectId
                  name = name
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered
                  projectId = $eventSourceId
                  name = name
                from ProjectNamed
                  name = name
                from ProjectRenamed key projectId
                  name = name
        """;

    const string PinnedClassic = "C6C2191FAE8B004ED6895499BE6640E572FF4DE006ABDABCB2DA1ADAD21D0122";

    static string Bytes(ArtifactRenderPlan plan) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(Code(plan))));

    internal static ArtifactRenderPlan Plan(string source, bool modern) =>
        Plan(source, modern ? CratisRendering.CreateNewApplicationProfile("Projects", new("Projects", "Projects")) : CratisRendering.CreateProfile("Projects", new("Projects", "Projects")));

    internal static ArtifactRenderPlan Plan(string source, ArtifactRenderProfile profile)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model-bound"), "model-bound", "Scopes.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        return new CratisArtifactRenderPlanner().Plan(new ArtifactRenderRequest(model, execution.Plan, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id)));
    }

    internal static string Code(ArtifactRenderPlan plan)
    {
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath == File).Bytes.AsSpan());
    }

    [Fact]
    public void should_render_attributes_instead_of_a_fluent_class()
    {
        var code = Code(Plan(Source, true));
        Assert.Contains("[FromEvent<ProjectRegistered>]", code, StringComparison.Ordinal);
        Assert.Contains("FromEvent<ProjectRenamed>(nameof(ProjectRenamed.ProjectId))", code, StringComparison.Ordinal);
        Assert.Contains("[NoAutoMap]", code, StringComparison.Ordinal);
        Assert.Contains("SetFromContext<ProjectNamed>(\"EventSourceId\")", code, StringComparison.Ordinal);
        Assert.Contains("SetFrom<ProjectNamed>(nameof(ProjectNamed.Name))", code, StringComparison.Ordinal);
        Assert.Contains("SetFrom<ProjectRenamed>(nameof(ProjectRenamed.ProjectId))", code, StringComparison.Ordinal);
        Assert.DoesNotContain("IProjectionFor<ProjectSummary>", code, StringComparison.Ordinal);
        Assert.DoesNotContain("#pragma warning disable", code, StringComparison.Ordinal);
        Assert.DoesNotContain("NoAutoMap()", code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task should_not_change_the_bytes_of_existing_conventions()
    {
        var classic = Bytes(Plan(Source, false));
        Assert.True(classic == PinnedClassic, "PIN " + classic);
        var convention = ReducerConvention.Classic with { SelfDefault = true };
        var options = new CratisRenderingOptions("Projects", "Projects");
        var v1 = CratisRendering.WithReducerConvention(CratisRendering.CreateProfile("Projects", options), ReducerConventionInput.Create("Projects", convention));
        Assert.Equal("1", v1.Inputs.Single(input => input.Name == ReducerConventionInput.Name).Version);
        Assert.Equal(PinnedClassic, Bytes(Plan(Source, v1)));
        var loaded = await when_rendering_a_pure_reducer.Load(Source + "\n" + """
                slice StateView Folded
                  readmodel FoldedSummary
                    projectId ProjectId
                    name ProjectName
                  query FoldedById => FoldedSummary?
                    by projectId ProjectId
                  reducer Fold => FoldedSummary
                    on ProjectRegistered
                      ```csharp
                      return new FoldedSummary(ProjectId: context.Event.ProjectId, Name: context.Event.Name);
                      ```
            """);
        var model = loaded.Model;
        var reducer = model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Reducers).Single();
        var v2 = CratisRendering.WithReducerConvention(CratisRendering.CreateProfile("Projects", options), ReducerConventionInput.Create("Projects", convention with
        {
            Overrides = System.Collections.Immutable.ImmutableDictionary<SemanticId, ReducerOverride>.Empty.Add(reducer.ReadModel, new("Projects.Projects.Registration.Folded.Fold", "Projects/Registration/Folded/Fold.cs"))
        }));
        Assert.Equal("2", v2.Inputs.Single(input => input.Name == ReducerConventionInput.Name).Version);
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan, v2, new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        };
        Assert.Equal(PinnedClassic, Bytes(new CratisArtifactRenderPlanner().Plan(request)));
    }

    [Fact]
    public void should_keep_fluent_projections_for_classic_applications()
    {
        var code = Code(Plan(Source, false));
        Assert.Contains("IProjectionFor<ProjectSummary>", code, StringComparison.Ordinal);
        Assert.Contains("#pragma warning disable CHR0029", code, StringComparison.Ordinal);
    }

    [Fact]
    public void should_keep_joins_children_and_nested_scopes_fluent() =>
        Assert.Equal(Code(Plan(when_rendering_scoped_projections.ScopedSource, false)), Code(Plan(when_rendering_scoped_projections.ScopedSource, true)));

    [Theory]
    [InlineData("remove")]
    [InlineData("unmapped")]
    public void should_keep_fluent_for_shapes_model_bound_cannot_express(string shape)
    {
        var source = shape switch
        {
            "remove" => Source + "\n        remove with ProjectRemoved",
            "unmapped" => Source.Replace("        name ProjectName\n      query ProjectById", "        name ProjectName\n        label ProjectName?\n      query ProjectById", StringComparison.Ordinal),
            _ => Source + "\n        from ProjectRegistered key projectId\n          name = name"
        };
        var code = Code(Plan(source, true));
        Assert.Contains("IProjectionFor<ProjectSummary>", code, StringComparison.Ordinal);
    }

    [Fact]
    public void should_express_the_counter_shape_only_from_version_four()
    {
        var source = Source.Replace("        name ProjectName\n      query ProjectById", "        name ProjectName\n        visits Decimal?\n      query ProjectById", StringComparison.Ordinal)
            .Replace("        from ProjectNamed\n          name = name\n", "        from ProjectNamed\n          name = name\n          increment visits\n", StringComparison.Ordinal);
        Assert.Contains("IProjectionFor<ProjectSummary>", Code(Plan(source, when_rendering_model_bound_running_totals.VersionThree())), StringComparison.Ordinal);
        var modern = Code(Plan(source, true));
        Assert.Contains("Increment<ProjectNamed>", modern, StringComparison.Ordinal);
        Assert.DoesNotContain("IProjectionFor<ProjectSummary>", modern, StringComparison.Ordinal);
    }
}
