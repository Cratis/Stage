// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

public class a_command_only_plan : Specification
{
    protected SemanticExecutionPlan _plan = null!;
    protected ExecutableSemanticModel _originalModel = null!;
    protected SemanticSpecification _specification = null!;
    protected SemanticSpecification _original = null!;
    protected virtual CanonicalCorpusVector Corpus => RegisterProjectCorpus.V2;

    protected void Establish()
    {
        var corpus = Corpus;
        var form = corpus.SourceForms[0];
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compiled = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        var original = compiled.Value!.Model;
        _originalModel = original;
        var slice = original.Application.Modules.Single().Features.Single().Slices.Single(candidate => candidate.Kind == SemanticSliceKind.StateChange);
        _original = slice.Specifications.Single(specification => specification.Name == "RegisteringAProject");
        _specification = _original with { ThenReadModels = [], ThenQueries = [] };
        _plan = Replace(original, slice, _specification);
    }

    protected SemanticExecutionPlan With(SemanticSpecification specification)
    {
        var model = _plan.Model;
        var slice = model.Application.Modules.Single().Features.Single().Slices.Single(candidate => candidate.Kind == SemanticSliceKind.StateChange);
        return Replace(model, slice, specification);
    }

    protected SemanticExecutionPlan WithProjection(SemanticSpecification specification) => Replace(_originalModel, _originalModel.Application.Modules.Single().Features.Single().Slices.Single(candidate => candidate.Kind == SemanticSliceKind.StateChange), specification, false);

    static SemanticExecutionPlan Replace(ExecutableSemanticModel model, SemanticSlice slice, SemanticSpecification specification, bool removeProjections = true)
    {
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var changedFeature = feature with { Slices = [.. feature.Slices.Select(candidate => candidate with { Projections = removeProjections ? [] : candidate.Projections, Specifications = [.. candidate.Specifications.Select(existing => existing.Id == specification.Id ? specification : existing)] })] };
        var application = model.Application with { Modules = [module with { Features = [changedFeature] }] };
        var changed = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
        return SemanticExecutionPlan.Compile(changed).Plan!;
    }
}
