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

public class when_rendering_with_metadata_for_an_unrelated_evolved_event : Specification
{
    SemanticCompilation _compilation = null!;
    ArtifactRenderProfile _profile = null!;
    AuthoringMetadataInput.Catalog _catalog = null!;
    SemanticEventContract _event = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        _compilation = when_rendering_authoring_documentation.Compile(when_rendering_authoring_documentation.Source + "\n" + """
                slice StateChange RenameProject
                  event ProjectRenamed generation 2
                    description "The current explanation"
                    documentation
                      ```markdown
                      Current documentation.
                      ```
                    name String
                  event ProjectRenamed generation 1
                    description "The historical explanation"
                    documentation
                      ```markdown
                      Historical documentation.
                      ```
                    name String
            """);
        _event = _compilation.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "RenameProject").Events.Single();
        _profile = CratisRendering.WithAuthoringMetadata(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), _compilation);
        AuthoringMetadataInput.TryRead(_profile.Inputs.Single(input => input.Name == AuthoringMetadataInput.Name), _compilation.Model, out var catalog).ShouldBeTrue();
        _catalog = catalog!;
    }

    void Because()
    {
        var slice = _compilation.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "RegisterProject");
        _plan = new CratisArtifactRenderPlanner().Plan(new(
            _compilation.Model,
            SemanticExecutionPlan.Compile(_compilation.Model).Plan!,
            _profile,
            new(ArtifactRenderScopeKind.Slice, slice.Id)));
    }

    [Fact] void should_use_the_semantic_current_revision() => _event.Revision.Value.ShouldEqual(2u);
    [Fact] void should_preserve_only_the_current_event_metadata() => _catalog.Entries[_event.Id.ToString()].ShouldEqual(new AuthoringMetadataInput.Metadata("The current explanation", "Current documentation."));
    [Fact] void should_ignore_historical_event_metadata() => _catalog.Entries.Values.ShouldNotContain(new AuthoringMetadataInput.Metadata("The historical explanation", "Historical documentation."));
    [Fact] void should_admit_the_unrelated_slice() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
    [Fact] void should_render_the_unrelated_slice_description() => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("RegisterProject.cs", StringComparison.Ordinal)).Bytes.AsSpan()).ShouldContain("/// A &lt;project&gt; &amp; name were registered");
}
