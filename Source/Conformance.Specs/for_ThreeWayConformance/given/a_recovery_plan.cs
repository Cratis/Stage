// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public abstract class a_recovery_plan : Specification
{
    protected ExecutableSemanticModel _model = null!;
    protected SemanticExecutionPlan _plan = null!;
    protected SemanticSpecification _specification = null!;
    protected SemanticSpecificationRun _reference = null!;
    protected SemanticSpecificationRunRecord _stage = null!;

    void Establish()
    {
        _model = SemanticModelSerializer.Deserialize(RecoverySpecificationsCorpus.V17.EsmBytes.AsSpan());
        _plan = SemanticExecutionPlan.Compile(_model).Plan!;
        _specification = _plan.Specifications.Values.Single(specification => specification.Name == "RedeliveryCascade");
    }

    protected void Select(SemanticSpecification specification)
    {
        _model = WithSpecification(_model, specification);
        _plan = SemanticExecutionPlan.Compile(_model).Plan!;
        _specification = specification;
    }

    protected async Task Execute()
    {
        _reference = new SemanticSpecificationRunner().Run(_plan, _specification.Id);
        _stage = (await new SemanticSpecificationExecutor().Run(_plan, new([_specification.Id]), new())).Results.Single();
    }

    protected void AssertParity()
    {
        Assert.True(_reference.Passed, string.Join(';', _reference.Failures));
        Assert.True(_stage.Outcome == SemanticSpecificationOutcome.Passed, $"{_stage.Unsupported?.Details} {string.Join(';', _stage.Failures)}");
        _stage.ExecutionKind.ShouldEqual(_reference.Execution.Kind.ToString());
        if (_reference.Execution is SemanticAccepted accepted)
        {
            Assert.Equal(accepted.Facts.Select(fact => fact.EventContract.ToString()), _stage.Trace!.Facts.Select(fact => fact.EventContract));
            Assert.Equal(accepted.Facts.Select(fact => fact.Route), _stage.Trace.Facts.Select(fact => fact.Route));
        }
    }

    internal static ExecutableSemanticModel WithSpecification(ExecutableSemanticModel model, SemanticSpecification specification) =>
        ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [.. model.Application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with
                    {
                        Specifications = slice.Specifications.Any(existing => existing.Id == specification.Id) ? [specification] : []
                    })]
                })]
            })]
        });

    internal static ExecutableSemanticModel Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Recovery"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("recovery"), "recovery", "Recovery.play", source);
        var compilation = new SemanticModelCompiler().Compile("Recovery", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));

        return compilation.Value!.Model;
    }
}
#endif
