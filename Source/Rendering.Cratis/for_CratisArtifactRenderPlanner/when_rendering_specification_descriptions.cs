// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_specification_descriptions : Specification
{
    internal const string Description = "Witnesses <registration> & lookup";
    internal const string Source = """
        concept ProjectId : Uuid
        concept ProjectName : String
        policy Clerks
          require authenticated
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
              specification RegisteringAProject
                description "Witnesses <registration> & lookup"
                given caller
                  authenticated
                when RegisterProject
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                then ProjectRegistered
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                then readmodel ProjectSummary
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                then query ProjectById
                  arguments
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  result
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    name = "Screenplay"
                then query ProtectedProjectById
                  arguments
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  result
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    name = "Screenplay"
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
                name ProjectName
              query ProjectById => ProjectSummary?
                by projectId ProjectId
              query ProtectedProjectById => ProjectSummary?
                by projectId ProjectId
                authorize Clerks
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key projectId
                  projectId = projectId
                  name = name
              specification LookingUpProject
                description "Witnesses <registration> & lookup"
                given readmodel ProjectSummary
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Pinned"
                then query ProjectById
                  arguments
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  result
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    name = "Pinned"
              specification DenyingAnonymousLookup
                description "Witnesses <registration> & lookup"
                given caller
                then query ProtectedProjectById
                  arguments
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then denied
        """;

    ArtifactRenderPlan _plan = null!;
    SemanticCompilation _compilation = null!;
    RenderedFile[] _sources = null!;

    void Establish() => _compilation = when_rendering_authoring_documentation.Compile(Source);

    void Because()
    {
        _plan = when_rendering_authoring_documentation.Plan(_compilation);
        Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
        _sources = [.. _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
    }

    [Fact] void should_describe_the_command_scenario() => Described("when_registering_aproject.cs");
    [Fact] void should_describe_the_read_model_scenario() => Described("when_registering_aproject_is_projected.cs");
    [Fact] void should_describe_the_query_scenario() => Described("when_registering_aproject_is_queried_through_project_by_id.cs");
    [Fact] void should_describe_the_protected_query_scenario() => Described("when_registering_aproject_is_queried_through_protected_project_by_id.cs");
    [Fact] void should_describe_the_seeded_query_scenario() => Described("when_looking_up_project_is_queried.cs");
    [Fact] void should_describe_the_denied_query_scenario() => Described("when_denying_anonymous_lookup_is_queried.cs");
    [Fact] void should_compile_the_generated_application_and_debug_specifications() => RenderedOutput.Errors(_sources).ShouldBeEmpty();
    [Fact] void should_compile_without_warnings() => RenderedOutput.Warnings(_sources).ShouldBeEmpty();

    [Fact]
    void should_change_specification_artifacts_without_changing_the_executable_revision()
    {
        var changed = when_rendering_authoring_documentation.Compile(Source.Replace(Description, "A different explanation", StringComparison.Ordinal));
        changed.Model.Revision.ShouldEqual(_compilation.Model.Revision);
        var changedPlan = when_rendering_authoring_documentation.Plan(changed);
        Assert.True(changedPlan.Success, string.Join("; ", changedPlan.Diagnostics));
        foreach (var artifact in _plan.Artifacts)
        {
            var changedArtifact = changedPlan.Artifacts.Single(candidate => candidate.RelativePath == artifact.RelativePath);
            if (artifact.RelativePath.Contains("/when_", StringComparison.Ordinal))
            {
                changedArtifact.Sha256.ShouldNotEqual(artifact.Sha256);
            }
            else
            {
                changedArtifact.Sha256.ShouldEqual(artifact.Sha256);
            }
        }
    }

    [Fact]
    void should_preserve_all_artifact_bytes_without_descriptions()
    {
        var compilation = when_rendering_authoring_documentation.Compile(Source.Replace($"        description \"{Description}\"\n", string.Empty, StringComparison.Ordinal));
        var withSyntax = when_rendering_authoring_documentation.Plan(compilation);
        var withoutSyntax = CratisRendering.Plan(
            compilation.Model,
            SemanticExecutionPlan.Compile(compilation.Model).Plan!,
            new(ArtifactRenderScopeKind.Application, compilation.Model.Application.Id),
            new("Projects", "Projects"));
        Assert.True(withSyntax.Success, string.Join("; ", withSyntax.Diagnostics));
        Assert.True(withoutSyntax.Success, string.Join("; ", withoutSyntax.Diagnostics));
        Assert.Equal(withoutSyntax.Artifacts.Select(artifact => artifact.Sha256), withSyntax.Artifacts.Select(artifact => artifact.Sha256));
    }

    [Fact]
    void should_normalize_unicode_line_breaks_in_every_scenario_summary()
    {
        var source = Source.Replace(Description, Description + "\u0085Next\u2028Last\u2029End  ", StringComparison.Ordinal);
        var plan = when_rendering_authoring_documentation.Plan(when_rendering_authoring_documentation.Compile(source));
        Assert.True(plan.Success, string.Join("; ", plan.Diagnostics));
        foreach (var artifact in plan.Artifacts.Where(artifact => artifact.RelativePath.Contains("/when_", StringComparison.Ordinal)))
        {
            Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).ShouldContain("/// Witnesses &lt;registration&gt; &amp; lookup\n/// Next\n/// Last\n/// End\n/// </summary>\npublic class ");
        }
    }

    void Described(string name) => _sources.Single(file => file.RelativePath.EndsWith(name, StringComparison.Ordinal)).Content
        .ShouldContain("/// <summary>\n/// Witnesses &lt;registration&gt; &amp; lookup\n/// </summary>\npublic class ");
}
