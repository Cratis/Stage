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
    protected SemanticSpecification _specification = null!;
    protected SemanticSpecification _original = null!;

    protected void Establish()
    {
        var corpus = RegisterProjectCorpus.V2;
        var form = corpus.SourceForms.Single();
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var compiled = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        var original = compiled.Value!.Model;
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

    static SemanticExecutionPlan Replace(ExecutableSemanticModel model, SemanticSlice slice, SemanticSpecification specification)
    {
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var changedFeature = feature with { Slices = [.. feature.Slices.Select(candidate => candidate.Id == slice.Id ? candidate with { Specifications = [.. candidate.Specifications.Select(existing => existing.Id == specification.Id ? specification : existing)] } : candidate)] };
        var application = model.Application with { Modules = [module with { Features = [changedFeature] }] };
        var changed = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
        return SemanticExecutionPlan.Compile(changed).Plan!;
    }
}
