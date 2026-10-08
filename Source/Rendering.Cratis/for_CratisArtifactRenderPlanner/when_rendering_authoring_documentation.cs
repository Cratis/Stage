// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_authoring_documentation : Specification
{
    internal const string Source = """
        concept ProjectId : Uuid
        concept ProjectName : String
        type ProjectInfo
          description "Describes <project> & details"
          name ProjectName
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                description "Registers <project> & name"
                documentation
                  ```markdown
                  # Command <notes> & details

                  - **Keep** the requested name.
                  ```
                projectId ProjectId identifier
                name ProjectName
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                description "A <project> & name were registered"
                documentation
                  ```markdown
                  # Registration <notes> & details

                  - **Keep** the project identity.
                  - `name` may contain <markup> & text.
                  ```
                projectId ProjectId
                name ProjectName
            slice StateView ProjectLookup
              readmodel ProjectSummary
                description "Shows <project> & name"
                documentation
                  ```markdown
                  # View <notes> & details

                  - `name` may contain <markup> & text.
                  ```
                projectId ProjectId
                name ProjectName
              query ProjectById => ProjectSummary?
                description "Finds <project> & name"
                by projectId ProjectId
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key projectId
                  projectId = projectId
                  name = name
        """;

    ArtifactRenderPlan _plan = null!;
    SemanticCompilation _compilation = null!;
    string _command = null!;
    string _view = null!;
    string _type = null!;

    void Establish() => _compilation = Compile(Source);

    void Because()
    {
        _plan = Plan(_compilation);
        Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
        _command = Text(_plan, "RegisterProject.cs");
        _view = Text(_plan, "ProjectLookup.cs");
        _type = Text(_plan, "ProjectInfo.cs");
    }

    [Fact] void should_render_the_command_description() => _command.ShouldContain("/// Registers &lt;project&gt; &amp; name\n/// </summary>");
    [Fact] void should_render_command_markdown_before_attributes() => _command.ShouldContain("/// <remarks>\n/// # Command &lt;notes&gt; &amp; details\n///\n/// - **Keep** the requested name.\n/// </remarks>\n[global::Cratis.Arc.Commands.ModelBound.CommandAttribute]");
    [Fact] void should_replace_the_event_summary() => _command.ShouldContain("/// A &lt;project&gt; &amp; name were registered");
    [Fact] void should_not_invent_an_event_summary_with_metadata() => _command.ShouldNotContain("The event that occurs when");
    [Fact] void should_copy_markdown_as_escaped_text() => _command.ShouldContain("/// <remarks>\n/// # Registration &lt;notes&gt; &amp; details\n///\n/// - **Keep** the project identity.\n/// - `name` may contain &lt;markup&gt; &amp; text.\n/// </remarks>");
    [Fact] void should_render_the_read_model_description() => _view.ShouldContain("/// Shows &lt;project&gt; &amp; name\n/// </summary>");
    [Fact] void should_render_read_model_markdown_before_attributes() => _view.ShouldContain("/// <remarks>\n/// # View &lt;notes&gt; &amp; details\n///\n/// - `name` may contain &lt;markup&gt; &amp; text.\n/// </remarks>\n[global::Cratis.Chronicle.Projections.ModelBound.FromEventAttribute");
    [Fact] void should_render_the_query_description() => _view.ShouldContain("    /// Finds &lt;project&gt; &amp; name\n    /// </summary>\n    [global::Cratis.Arc.Authorization");
    [Fact] void should_render_the_composite_type_description() => _type.ShouldContain("/// Describes &lt;project&gt; &amp; details");
    [Fact] void should_render_identical_bytes_twice() => Assert.Equal(_plan.Artifacts.Select(artifact => artifact.Sha256), Plan(_compilation).Artifacts.Select(artifact => artifact.Sha256));

    [Fact]
    void should_invalidate_artifacts_without_changing_the_executable_revision()
    {
        var changed = Compile(Source.Replace("A <project> & name were registered", "Registered with a different explanation", StringComparison.Ordinal));
        changed.Model.Revision.ShouldEqual(_compilation.Model.Revision);
        Plan(changed).Artifacts.Single(artifact => artifact.RelativePath.EndsWith("RegisterProject.cs", StringComparison.Ordinal)).Sha256
            .ShouldNotEqual(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("RegisterProject.cs", StringComparison.Ordinal)).Sha256);
    }

    [Fact]
    void should_invalidate_command_documentation_without_changing_the_executable_revision()
    {
        var changed = Compile(Source.Replace("# Command <notes> & details", "# Changed command notes", StringComparison.Ordinal));
        changed.Model.Revision.ShouldEqual(_compilation.Model.Revision);
        Text(Plan(changed), "RegisterProject.cs").ShouldNotEqual(_command);
        Text(Plan(changed), "ProjectLookup.cs").ShouldEqual(_view);
    }

    [Fact]
    void should_invalidate_read_model_documentation_without_changing_the_executable_revision()
    {
        var changed = Compile(Source.Replace("# View <notes> & details", "# Changed view notes", StringComparison.Ordinal));
        changed.Model.Revision.ShouldEqual(_compilation.Model.Revision);
        Text(Plan(changed), "ProjectLookup.cs").ShouldNotEqual(_view);
        Text(Plan(changed), "RegisterProject.cs").ShouldEqual(_command);
    }

    [Fact]
    void should_keep_existing_bytes_when_metadata_is_absent()
    {
        var source = Source.Replace("        documentation\n          ```markdown\n          # Command <notes> & details\n\n          - **Keep** the requested name.\n          ```\n", string.Empty, StringComparison.Ordinal)
            .Replace("        documentation\n          ```markdown\n          # View <notes> & details\n\n          - `name` may contain <markup> & text.\n          ```\n", string.Empty, StringComparison.Ordinal)
            .Replace("  description \"Describes <project> & details\"\n", string.Empty, StringComparison.Ordinal)
            .Replace("        description \"Registers <project> & name\"\n", string.Empty, StringComparison.Ordinal)
            .Replace("        description \"A <project> & name were registered\"\n", string.Empty, StringComparison.Ordinal)
            .Replace("        documentation\n          ```markdown\n          # Registration <notes> & details\n\n          - **Keep** the project identity.\n          - `name` may contain <markup> & text.\n          ```\n", string.Empty, StringComparison.Ordinal)
            .Replace("        description \"Shows <project> & name\"\n", string.Empty, StringComparison.Ordinal)
            .Replace("        description \"Finds <project> & name\"\n", string.Empty, StringComparison.Ordinal);
        var compilation = Compile(source);
        var withoutSyntax = CratisRendering.Plan(
            compilation.Model,
            SemanticExecutionPlan.Compile(compilation.Model).Plan!,
            new(ArtifactRenderScopeKind.Application, compilation.Model.Application.Id),
            new("Projects", "Projects"));
        Assert.Equal(withoutSyntax.Artifacts.Select(artifact => artifact.Sha256), Plan(compilation).Artifacts.Select(artifact => artifact.Sha256));
    }

    [Fact]
    void should_reject_an_unknown_metadata_version()
    {
        var profile = CratisRendering.WithAuthoringMetadata(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), _compilation);
        var metadata = profile.Inputs.Single(input => input.Name == Semantics.AuthoringMetadataInput.Name);
        var changed = ArtifactRenderProfile.Create(
            profile.Target,
            profile.TargetVersion,
            profile.Renderer,
            profile.RendererVersion,
            [.. profile.Inputs.Where(input => input != metadata), ArtifactRenderInput.Create(metadata.Name, "999", metadata.Bytes)]);
        var plan = new CratisArtifactRenderPlanner().Plan(new(
            _compilation.Model,
            SemanticExecutionPlan.Compile(_compilation.Model).Plan!,
            changed,
            new(ArtifactRenderScopeKind.Application, _compilation.Model.Application.Id)));
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
    }

    internal static SemanticCompilation Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("documentation"), "documentation", "Projects.play", source);
        var result = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(result.Success, string.Join("; ", result.Diagnostics));
        return result.Value!;
    }

    internal static ArtifactRenderPlan Plan(SemanticCompilation compilation)
    {
        var profile = CratisRendering.WithAuthoringMetadata(
            CratisRendering.CreateProfile(compilation.Model.Application.Name, new("Projects", "Projects")),
            compilation);
        return new CratisArtifactRenderPlanner().Plan(new(
            compilation.Model,
            SemanticExecutionPlan.Compile(compilation.Model).Plan!,
            profile,
            new(ArtifactRenderScopeKind.Application, compilation.Model.Application.Id)));
    }

    static string Text(ArtifactRenderPlan plan, string name) => System.Text.Encoding.UTF8.GetString(
        plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith(name, StringComparison.Ordinal)).Bytes.AsSpan());
}
