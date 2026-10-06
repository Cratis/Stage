// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_an_evolved_event : a_v4_reducer
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = new CratisArtifactRenderPlanner().Plan(_request);

    [Fact] void should_admit_v4() => _request.Model.SemanticVersion.ShouldEqual(SemanticVersion.V4);
    [Fact] void should_render_the_current_event_and_reducer() => Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
    [Fact] void should_emit_no_historical_property() => _plan.Artifacts.All(artifact => !Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("OldAmount", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_compile_the_current_generation_output()
    {
        var files = _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        var errors = RenderedOutput.Errors(files);
        Assert.True(_plan.Success && errors.Count == 0, string.Join(Environment.NewLine, errors));
    }
}
