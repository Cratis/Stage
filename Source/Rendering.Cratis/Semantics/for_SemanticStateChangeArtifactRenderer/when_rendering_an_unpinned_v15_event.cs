// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer;

public class when_rendering_an_unpinned_v15_event : Specification
{
    ExecutableSemanticModel _model = null!;
    string _code = null!;

    void Establish() => _model = a_generation_model.Model();
    void Because()
    {
        var options = new CratisRenderingOptions("GenerationApp", "GenerationApp");
        var context = new SemanticApplicationContext(new(_model, SemanticExecutionPlan.Compile(_model).Plan, CratisRendering.CreateProfile(_model.Application.Name, options), new(ArtifactRenderScopeKind.Application, _model.Application.Id)), options);
        var located = context.SelectedSlices().Single(slice => slice.Slice.Name == "Prior");
        var builder = new CSharpCodeBuilder();
        SemanticStateChangeArtifactRenderer.RenderMembers(builder, located with { Slice = located.Slice with { Commands = [] } }, context);
        _code = builder.ToString();
    }

    [Fact] void should_use_the_v15_model() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V15);
    [Fact] void should_preserve_the_previous_event_bytes() => _code.ShouldEqual("""
        /// <summary>
        /// Records that unrelated.
        /// </summary>
        [global::Cratis.Chronicle.Events.EventTypeAttribute]
        public record Unrelated();
        """ + "\n\n");
}
