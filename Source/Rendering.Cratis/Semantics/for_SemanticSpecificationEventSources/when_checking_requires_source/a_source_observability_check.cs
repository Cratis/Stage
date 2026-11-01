// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationEventSources.when_checking_requires_source;

public abstract class a_source_observability_check : Specification
{
    private protected ExecutableSemanticModel _model = null!;
    private protected SemanticSlice _slice = null!;
    private protected SemanticProjection _projection = null!;
    private protected SemanticProjectionScope _scope = null!;
    private protected bool _requiresSource;

    void Establish()
    {
        _model = invoice_model.Compile(when_checking_sourceless_event_consumers.Source);
        _slice = _model.Application.Modules.Single().Features.Single().Slices.Single();
        _projection = _slice.Projections.Single();
        _scope = _projection.Scope!;
    }

    private protected bool Check(SemanticSlice slice, params SemanticSlice[] others)
    {
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        var application = _model.Application with { Modules = [module with { Features = [feature with { Slices = [slice, .. others] }] }] };
        var model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, application);
        return Check(model, slice.Events[0].Id, slice.Id);
    }

    private protected static bool Check(ExecutableSemanticModel model, SemanticId contract, SemanticId slice) =>
        SemanticSpecificationEventSources.RequiresSource(CreateContext(model, slice), contract);

    private protected static SemanticApplicationContext CreateContext(ExecutableSemanticModel model, SemanticId slice)
    {
        var options = new CratisRenderingOptions("Payments", "Payments");
        return new SemanticApplicationContext(
            new(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Slice, slice)),
            options);
    }

    private protected bool Check(SemanticProjectionScope scope) => Check(_slice with { Projections = [_projection with { Scope = scope }] });
}
