// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class a_translation_application : Specification
{
    protected ExecutableSemanticModel _model = null!;
    protected ArtifactRenderPlan _plan = null!;
    protected Assembly _assembly = null!;
    protected RenderedFile[] _sources = [];

    void Establish()
    {
        _model = Transform(SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.ToArray()), slice => slice with { Specifications = [] });
    }

    protected void Render()
    {
        _plan = Plan(_model);
        Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        _sources = [.. _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
        _assembly = RenderedOutput.Load(_sources);
    }

    protected Type Generated(string slice, string name) => _assembly.GetType($"Contracts.Integration.Translation.{slice}.{name}", throwOnError: true)!;

    protected static ArtifactRenderPlan Plan(ExecutableSemanticModel model, ArtifactRefusalMode refusals = ArtifactRefusalMode.Refuse)
    {
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues.Select(issue => issue.Details)));
        var profile = CratisRendering.CreateProfile(model.Application.Name, new("Contracts", "Contracts"));
        if (refusals != ArtifactRefusalMode.Refuse)
        {
            profile = ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs, RefusalModeInput.Create(refusals)]);
        }
        return new CratisArtifactRenderPlanner().Plan(new(model, execution.Plan!, profile, new(ArtifactRenderScopeKind.Application, model.Application.Id)));
    }

    protected static ExecutableSemanticModel Transform(ExecutableSemanticModel model, Func<SemanticSlice, SemanticSlice> transform) => ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with { Features = Features(module.Features, transform) })]
        });

    static ImmutableArray<SemanticFeature> Features(ImmutableArray<SemanticFeature> features, Func<SemanticSlice, SemanticSlice> transform) =>
        [.. features.Select(feature => feature with { Slices = [.. feature.Slices.Select(transform)], Features = Features(feature.Features, transform) })];
}
#endif
