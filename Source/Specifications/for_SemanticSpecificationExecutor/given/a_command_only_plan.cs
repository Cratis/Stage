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

    protected SemanticExecutionPlan WithBehavior(SemanticSpecification specification, SemanticCommand? command = null, SemanticConstraint? constraint = null, SemanticPolicy? policy = null, bool keepProjections = false)
    {
        var model = keepProjections ? _originalModel : _plan.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var originalSlice = feature.Slices.Single(slice => slice.Kind == SemanticSliceKind.StateChange);
        SemanticSpecification Adapt(SemanticSpecification value)
        {
            if (value.Id == specification.Id) return specification;
            if (policy is not null && value.When?.Command == command?.Id) return value with { GivenCaller = new SemanticCaller(true, ["Registrar"], []) };
            return value;
        }
        var updatedSlice = originalSlice with
        {
            Commands = command is null ? originalSlice.Commands : [.. originalSlice.Commands.Select(value => value.Id == command.Id ? command : value)],
            Constraints = constraint is null ? originalSlice.Constraints : [constraint],
            Specifications = [.. originalSlice.Specifications.Select(Adapt)]
        };
        var application = model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(slice => slice.Id == originalSlice.Id ? updatedSlice : slice)] }] }],
            Policies = policy is null ? model.Application.Policies : [.. model.Application.Policies, policy]
        };
        return SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
    }

    protected SemanticExecutionPlan WithProjection(SemanticSpecification specification) => Replace(_originalModel, _originalModel.Application.Modules.Single().Features.Single().Slices.Single(candidate => candidate.Kind == SemanticSliceKind.StateChange), specification, false);

    // RegisterProject V2 now has two event generations and is ESM v4. These attachment tests
    // exercise v3 behavior, so use its current event shape as a single-generation contract.
    protected static ExecutableSemanticModel CreateV3(SemanticApplication application)
    {
        var modules = application.Modules.Select(module => module with
        {
            Features = [.. module.Features.Select(feature => feature with
            {
                Slices = [.. feature.Slices.Select(slice => slice with
                {
                    Events = [.. slice.Events.Select(@event => @event with
                    {
                        Revision = EventContractRevision.Initial,
                        Predecessor = null,
                        PriorRevisions = []
                    })]
                })]
            })]
        });
        return ExecutableSemanticModel.Create(LanguageVersion.V3, SemanticVersion.V3, application with { Modules = [.. modules] });
    }

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
