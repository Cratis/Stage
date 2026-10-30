// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_selecting_model_bound_projections : Specification
{
    internal const string PayloadSource = """
        concept ProjectId : Uuid
        module Projects
          feature Registration
            slice StateChange CreateProject
              command CreateProject
                id ProjectId identifier
                name String
                produces ProjectCreated
                  for id
                  id = id
                  name = name
              event ProjectCreated
                id ProjectId
                name String
              event ProjectChanged
                id ProjectId
                title String
              event ProjectRemoved
                id ProjectId
            slice StateView Lookup
              readmodel Summary
                id ProjectId key
                name String optional
                visits Int optional
              query ById => Summary optional
                by id ProjectId
              projection SummaryProjection => Summary
                no automap
                from ProjectCreated key id
                  id = id
                  name = name
                  increment visits
                from ProjectChanged key id
                  name = title
                  decrement visits
                remove with ProjectRemoved key id
        """;

    internal static readonly string Source = PayloadSource
        .Replace("          id = id\n", string.Empty, StringComparison.Ordinal)
        .Replace("        id ProjectId\n", string.Empty, StringComparison.Ordinal)
        .Replace("from ProjectCreated key id", "from ProjectCreated\n          id = $eventSourceId", StringComparison.Ordinal)
        .Replace("from ProjectChanged key id", "from ProjectChanged", StringComparison.Ordinal)
        .Replace("remove with ProjectRemoved key id", "remove with ProjectRemoved", StringComparison.Ordinal);

    [Fact]
    public void should_use_attributes_and_automap_only_when_equivalent()
    {
        var plan = Plan(Source, "model-bound");
        var code = Code(plan);
        Assert.DoesNotContain("IProjectionFor", code);
        Assert.DoesNotContain("#pragma", code);
        Assert.DoesNotContain("NoAutoMap", code);
        Assert.DoesNotContain("nameof(global::Projects.Projects.Registration.CreateProject.ProjectCreated.Name)", code);
        Assert.Contains("SetFromAttribute<global::Projects.Projects.Registration.CreateProject.ProjectChanged>(nameof(global::Projects.Projects.Registration.CreateProject.ProjectChanged.Title))", code);
        Assert.Contains("IncrementAttribute<", code);
        Assert.Contains("DecrementAttribute<", code);
        Assert.Contains("RemovedWithAttribute<", code);
        Assert.Contains("SetFromContextAttribute<global::Projects.Projects.Registration.CreateProject.ProjectCreated>(\"EventSourceId\")", code);
        Assert.Empty(RenderedOutput.Errors(Files(plan)));
        Assert.Empty(RenderedOutput.Warnings(Files(plan)));
    }

    [Fact]
    public void should_disable_automap_for_an_unintended_same_name_payload()
    {
        var source = Source.Replace("        name String\n      event", "        name String\n        visits Int\n      event", StringComparison.Ordinal)
            .Replace("          name = name\n      event", "          name = name\n          visits = 0\n      event", StringComparison.Ordinal);
        var code = Code(Plan(source, "model-bound"));
        Assert.Contains("NoAutoMapAttribute", code);
        Assert.Contains("ProjectCreated.Name)", code);
    }

    [Fact]
    public void should_lower_literal_and_context_mappings()
    {
        var source = Source.Replace("name = title", "name = \"fixed\"", StringComparison.Ordinal);
        var plan = Plan(source, "model-bound");
        Assert.Contains("SetValueAttribute<global::Projects.Projects.Registration.CreateProject.ProjectChanged>(\"fixed\")", Code(plan));
        Assert.Contains("SetFromContextAttribute<", Code(plan));
        Assert.Empty(RenderedOutput.Errors(Files(plan)));
    }

    [Fact]
    public void should_keep_absent_and_classic_artifact_bytes_identical()
    {
        var absent = Plan(Source, null);
        var classic = Plan(Source, "classic");
        Assert.Contains("IProjectionFor", Code(classic));
        Assert.Contains("#pragma warning disable CHR0029, CHR0042", Code(classic));
        Assert.Equal(absent.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)), classic.Artifacts.Where(artifact => artifact.RelativePath != ProjectionConventionInput.RelativePath).Select(artifact => (artifact.RelativePath, artifact.Sha256)));
    }

    [Fact]
    public void should_restore_the_scaffolded_new_application_conventions()
    {
        var options = new CratisRenderingOptions("Projects", "Projects");
        var profile = CratisRendering.CreateNewApplicationProfile("Projects", options);
        var scaffold = CratisRendering.PlanScaffold(new("Projects", "Projects", "Projects") { Profile = profile });
        Assert.True(scaffold.Success, string.Join(Environment.NewLine, scaffold.Diagnostics));
        var projection = Assert.Single(scaffold.Artifacts, artifact => artifact.RelativePath == ProjectionConventionInput.RelativePath);
        var reducer = Assert.Single(scaffold.Artifacts, artifact => artifact.RelativePath == ReducerConventionInput.RelativePath);
        var restored = CratisRendering.WithProjectionConvention(
            CratisRendering.WithReducerConvention(CratisRendering.CreateProfile("Projects", options), ReducerConventionInput.Read("Projects", Text(reducer))),
            ProjectionConventionInput.Read("Projects", Text(projection)));
        Assert.Equal(profile.Inputs.Select(input => (input.Name, input.Version, input.Sha256)), restored.Inputs.Select(input => (input.Name, input.Version, input.Sha256)));
        var model = Compile(Source);
        Assert.Equal(Code(Plan(model, profile)), Code(Plan(model, restored)));
        Assert.DoesNotContain("IProjectionFor", Code(Plan(model, restored)));
    }

    internal static ArtifactRenderPlan Plan(string source, string? convention)
    {
        var profile = CratisRendering.CreateProfile("Projects", new("Projects", "Projects"));
        if (convention is not null)
        {
            profile = CratisRendering.WithProjectionConvention(profile, ProjectionConventionInput.Create("Projects", convention));
        }

        return Plan(Compile(source), profile);
    }

    internal static ExecutableSemanticModel Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model-bound"), "model-bound", "Projection.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        return compilation.Value!.Model;
    }

    static ArtifactRenderPlan Plan(ExecutableSemanticModel model, ArtifactRenderProfile profile)
    {
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        var request = new ArtifactRenderRequest(model, execution.Plan!, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return plan;
    }

    internal static string Code(ArtifactRenderPlan plan) => Text(Assert.Single(plan.Artifacts, artifact => artifact.RelativePath.EndsWith("/Lookup/Lookup.cs", StringComparison.Ordinal)));
    internal static IEnumerable<RenderedFile> Files(ArtifactRenderPlan plan) => plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
        .Select(artifact => new RenderedFile(artifact.RelativePath, Text(artifact)));
    static string Text(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
}
#endif
