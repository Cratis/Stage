// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_CratisEsmRangeAdmission.given;

public class a_range_model : Specification
{
    protected ExecutableSemanticModel _model = null!;
    protected ArtifactRenderRequest _request = null!;
    private protected SemanticApplicationContext _context = null!;

    void Establish() => SetModel(SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(32).Bytes.AsSpan()));

    protected SemanticFeature UseCaptureOnly()
    {
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        var capture = feature.Slices.Single(slice => slice.Kind == SemanticSliceKind.Translate) with { Specifications = [] };
        var reduced = feature with { Slices = [capture] };
        var application = _model.Application with { Modules = [module with { Features = [reduced] }] };
        SetModel(ExecutableSemanticModel.Create(LanguageVersion.V32, SemanticVersion.V32, application));
        return reduced;
    }

    protected void SetModel(ExecutableSemanticModel model)
    {
        _model = model;
        var options = new CratisRenderingOptions("Contracts", "Contracts");
        _request = new(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _context = new(_request, options);
    }

    protected void Versions(uint languageMajor, uint languageMinor, uint semanticMajor, uint semanticMinor)
    {
        typeof(ExecutableSemanticModel).GetField("<LanguageVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_model, new LanguageVersion(languageMajor, languageMinor));
        typeof(ExecutableSemanticModel).GetField("<SemanticVersion>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_model, new SemanticVersion(semanticMajor, semanticMinor));
    }
}
