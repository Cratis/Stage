// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics;
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

    protected void Render(bool clientOnly = false) => (_plan, _sources, _assembly) = RenderModel(_model, clientOnly);

    protected static (ArtifactRenderPlan Plan, RenderedFile[] Sources, Assembly Assembly) RenderModel(ExecutableSemanticModel model, bool clientOnly = false)
    {
        var plan = Plan(model);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        RenderedFile[] sources = [.. plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
        return (plan, sources, RenderedOutput.Load(sources, clientOnly));
    }

    private protected (SemanticApplicationContext Context, LocatedSemanticSlice Located, SemanticCapture Capture, SemanticId First, SemanticId Second) InternalCaptureProbe(bool sameNamespace, bool reverse = false)
    {
        var slices = _model.Application.Modules.Single().Features.Single().Slices;
        var source = slices.Single(slice => slice.Name == "TrackShipments").Events.Single(contract => contract.Origin is not null);
        var other = slices.Single(slice => slice.Name == "PackOrder").Events.Single() with
        {
            Name = sameNamespace ? "Shipment Dispatched" : source.Name,
            Origin = source.Origin,
            Visibility = source.Visibility,
            Properties = [.. source.Properties.Select(property => property with { Id = SemanticId.Create(SemanticKind.Property, $"second-source-{property.Name}") })]
        };
        var model = Transform(
            _model,
            slice => slice.Captures.IsEmpty ? slice with { Events = sameNamespace ? [] : [other], Commands = [] } : slice with
            {
                Events = sameNamespace ? slice.Events.Add(other) : slice.Events,
                Captures = [.. slice.Captures.Select(capture => capture with { Name = "OnShipmentDispatched", EventsSource = capture.EventsSource! with { Events = reverse ? [other.Id, source.Id] : [source.Id, other.Id] } })]
            },
            include: slice => slice.Name != "PublishOrderShipped");
        var options = new CratisRenderingOptions("Contracts", "Contracts");
        var context = new SemanticApplicationContext(new(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id)), options);
        var located = context.SelectedSlices().Single(slice => slice.Slice.Name == "TrackShipments");
        return (context, located, located.Slice.Captures.Single(), source.Id, other.Id);
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

    protected static ExecutableSemanticModel Transform(ExecutableSemanticModel model, Func<SemanticSlice, SemanticSlice> transform, bool recovery = false, Func<SemanticSlice, bool>? include = null) => ExecutableSemanticModel.Create(recovery ? LanguageVersion.V17 : model.LanguageVersion, recovery ? SemanticVersion.V17 : model.SemanticVersion, model.Application with
    {
        Modules = [.. model.Application.Modules.Select(module => module with { Features = Features(module.Features, transform, include) })]
    });

    static ImmutableArray<SemanticFeature> Features(ImmutableArray<SemanticFeature> features, Func<SemanticSlice, SemanticSlice> transform, Func<SemanticSlice, bool>? include) =>
        [.. features.Select(feature => feature with { Slices = [.. feature.Slices.Where(slice => include?.Invoke(slice) ?? true).Select(transform)], Features = Features(feature.Features, transform, include) })];
}
#endif
