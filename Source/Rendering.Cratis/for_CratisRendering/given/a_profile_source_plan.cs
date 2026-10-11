// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.given;

public class a_profile_source_plan : Specification
{
    protected ArtifactRenderProfile _profile = null!;
    protected LoadedSemanticModel _loaded = null!;
    protected CratisPlanOptions _options = new("Projects", "Projects", "Projects");
    protected PlanSelection _selection = new([PlanSelectionEntry.Feature("Projects", "Registration")]);
    protected ArtifactRenderPlan _full = null!;
    protected CratisPlanResult _result = null!;

    protected virtual string ModelSource => when_rendering_authoring_documentation.Source;

    void Establish()
    {
        var compilation = when_rendering_authoring_documentation.Compile(ModelSource);
        _loaded = new(compilation.Model, SemanticExecutionPlan.Compile(compilation.Model).Plan!);
        var stringsFiles = new Dictionary<string, string> { ["projects.en.strings"] = "project.name = \"Project name\"\n" };
        var profile = CratisRendering.CreateProfile("Projects", new("Projects", "Projects"), null, stringsFiles, "en");
        _profile = CratisRendering.WithAuthoringMetadata(profile, compilation);
        _options = _options with { Profile = _profile };
    }

    protected void PlanFull() => _full = new CratisArtifactRenderPlanner().Plan(new(
        _loaded.Model,
        _loaded.Plan,
        DomainPlacementInput.WithDomain(_profile, _options.Domain),
        new(ArtifactRenderScopeKind.Application, _loaded.Model.Application.Id)));

    protected void ShouldMatchFull()
    {
        _result.Artifacts.ShouldNotBeEmpty();
        _result.Artifacts.All(artifact => _full.Artifacts.Single(full => full.RelativePath == artifact.RelativePath).Bytes.SequenceEqual(artifact.Bytes)).ShouldBeTrue();
    }

    protected string CommandText() => Encoding.UTF8.GetString(_result.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("RegisterProject.cs", StringComparison.Ordinal)).Bytes.AsSpan());
}
