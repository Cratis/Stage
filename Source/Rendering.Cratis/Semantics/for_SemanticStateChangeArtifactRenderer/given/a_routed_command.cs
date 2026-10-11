// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.given;

public class a_routed_command : Specification
{
    protected ExecutableSemanticModel _model = null!;
    protected SemanticSlice _slice = null!;
    protected SemanticCommand _command = null!;
    protected SemanticEventSource _source = null!;
    protected ArtifactRenderPlan _plan = null!;
    protected string _code = null!;

    void Establish()
    {
        _model = invoice_model.Compile(when_planning_event_source_routes.Source);
        _slice = _model.Application.Modules[0].Features[0].Slices[0];
        _command = _slice.Commands[0];
        _source = _model.Application.EventSources[0];
    }

    protected void ReplaceSlice()
    {
        _slice = _slice with { Commands = [_command] };
        var module = _model.Application.Modules[0];
        var feature = module.Features[0] with { Slices = [_slice] };
        _model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with { EventSources = [_source], Modules = [module with { Features = [feature] }] });
    }

    protected void Plan()
    {
        ReplaceSlice();
        _plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model).Plan!, new(ArtifactRenderScopeKind.Application, _model.Application.Id), new("InvoiceApp", "InvoiceApp"));
        _plan.Success.ShouldBeTrue();
        _code = Text(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Deposit.cs", StringComparison.Ordinal)));
    }

    protected ImmutableArray<RenderedFile> Files() => [.. _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs" && !artifact.RelativePath.StartsWith("Frontend/", StringComparison.Ordinal)).Select(artifact => new RenderedFile(artifact.RelativePath, Text(artifact)))];

    protected Assembly Load() => RenderedOutput.Load(Files());

    protected static string Text(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
}
