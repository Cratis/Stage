// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_inline_and_file_parity
{
    [Fact]
    public void should_render_the_same_artifact_paths()
    {
        var inline = opaque_policy_model.Plan(reaction_body_model.Load(file: false));
        var file = opaque_policy_model.Plan(reaction_body_model.Load(file: true));
        Assert.True(inline.Success, opaque_policy_model.Errors(inline));
        Assert.True(file.Success, opaque_policy_model.Errors(file));
        Assert.Equal(inline.Artifacts.Select(artifact => artifact.RelativePath).Order(), file.Artifacts.Select(artifact => artifact.RelativePath).Order());
    }

    [Fact]
    public void should_render_byte_identical_reactors()
    {
        var inline = Reactor(reaction_body_model.Load(file: false));
        var file = Reactor(reaction_body_model.Load(file: true));
        Assert.Equal(inline, file);
    }

    static string Reactor(LoadedSemanticModel loaded)
    {
        var plan = opaque_policy_model.Plan(loaded);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        return Encoding.UTF8.GetString(Assert.Single(plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith("Notifier.cs", StringComparison.Ordinal))).Bytes.AsSpan());
    }
}
#endif
