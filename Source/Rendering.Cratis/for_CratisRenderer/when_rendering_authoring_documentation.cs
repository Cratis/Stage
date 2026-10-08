// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_authoring_documentation : Specification
{
    string _command = null!;
    string _view = null!;
    string _type = null!;
    string _viewWithoutDeclaration = null!;

    void Because()
    {
        // The legacy renderer needs a block-level key to identify the inferred model, not only an event-level key.
        var source = for_CratisArtifactRenderPlanner.when_rendering_authoring_documentation.Source.Replace(
            "from ProjectRegistered key projectId\n",
            "from ProjectRegistered\n          key projectId\n",
            StringComparison.Ordinal);
        var compilation = new ScreenplayCompiler().Compile(source);
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics));
        var application = compilation.Value!;
        var set = new ApplicationSet([application]);
        var slices = application.Modules.Single().Features.Single().Slices.ToArray();
        _command = new StateChangeSliceRenderer().Render(new LocatedSlice(slices[0], ["Projects", "Registration"]), set, "Projects").Content;
        _view = new StateViewSliceRenderer().Render(new LocatedSlice(slices[1], ["Projects", "Registration"]), set, "Projects").Content;
        _type = TypeRenderer.Render(application.Types.Single(), set, "Projects").Content;
        _viewWithoutDeclaration = new StateViewSliceRenderer().Render(new LocatedSlice(slices[1] with { ReadModels = null }, ["Projects", "Registration"]), set, "Projects").Content;
    }

    [Fact] void should_render_the_command_description() => _command.ShouldContain("/// Registers &lt;project&gt; &amp; name\n/// </summary>\n[Command]");
    [Fact] void should_render_the_event_description() => _command.ShouldContain("/// A &lt;project&gt; &amp; name were registered");
    [Fact] void should_copy_the_event_markdown_as_escaped_remarks() => _command.ShouldContain("/// <remarks>\n/// # Registration &lt;notes&gt; &amp; details\n///\n/// - **Keep** the project identity.");
    [Fact] void should_render_the_declared_read_model_description() => _view.ShouldContain("/// Shows &lt;project&gt; &amp; name");
    [Fact] void should_render_the_query_description() => _view.ShouldContain("    /// Finds &lt;project&gt; &amp; name\n    /// </summary>\n    [AllowAnonymous]");
    [Fact] void should_render_the_composite_type_description() => _type.ShouldContain("/// Describes &lt;project&gt; &amp; details");
    [Fact] void should_render_an_inferred_model_without_a_declared_read_model_collection() => _viewWithoutDeclaration.ShouldContain("public record ProjectSummary(");
}
