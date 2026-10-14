// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.given;

public class a_partitioned_screen_plan : Specification
{
    protected SemanticCompilation _compilation = null!;
    protected SceneApplication _scene = null!;
    protected ArtifactRenderProfile _legacy = null!;
    protected ArtifactRenderProfile _profile = null!;
    protected LoadedSemanticModel _loaded = null!;
    protected ArtifactRenderPlan _application = null!;
    protected CratisPlanResult _result = null!;
    protected CratisPlanOptions _options = new("Projects", "Projects", "Projects");

    protected static string Source => when_rendering_authoring_documentation.Source
        .Replace("    slice StateView ProjectLookup", "      screen StaticOverview\n        title \"Static overview\"\n    slice StateView ProjectLookup", StringComparison.Ordinal)
        + "\n      screen LookupOverview\n        action RegisterProject\n";

    protected virtual string ModelSource => Source;

    void Establish()
    {
        _compilation = when_rendering_authoring_documentation.Compile(ModelSource);
        var syntax = new ScreenplayCompiler().Compile(ModelSource);
        Assert.True(syntax.Success, string.Join(';', syntax.Diagnostics));
        _scene = new ScreenplaySceneVisitor().Visit(syntax.Value!);
        _legacy = CratisRendering.CreateProfile("Projects", new("Projects", "Projects"), _scene);
        _profile = CratisRendering.WithScreenPartitions(_legacy, _compilation);
        _loaded = new(_compilation.Model, SemanticExecutionPlan.Compile(_compilation.Model).Plan!);
        _options = _options with { Profile = _profile };
    }

    protected ArtifactRenderPlan AppPlan(ArtifactRenderProfile profile) => new CratisArtifactRenderPlanner().Plan(new(
        _loaded.Model, _loaded.Plan, profile, new(ArtifactRenderScopeKind.Application, _loaded.Model.Application.Id)));

    protected static IEnumerable<PlannedArtifact> Fragments(IEnumerable<PlannedArtifact> artifacts) => artifacts.Where(artifact => artifact.RelativePath.EndsWith("/Screens.scene.json", StringComparison.Ordinal));
    protected static string Text(PlannedArtifact artifact) => System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
    protected SemanticSlice Slice(string name) => _loaded.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == name);
    protected ArtifactRenderInput Partitions => _profile.Inputs.Single(input => input.Name == ScreenPartitionsInput.Name);
    protected ArtifactRenderProfile WithInput(ArtifactRenderInput input) => ArtifactRenderProfile.Create(_profile.Target, _profile.TargetVersion, _profile.Renderer, _profile.RendererVersion, [.. _profile.Inputs.Where(existing => existing.Name != ScreenPartitionsInput.Name), input]);
}
