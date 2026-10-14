// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

public abstract class a_recovery_plan : Specification
{
    protected ExecutableSemanticModel _model = null!;
    protected SemanticExecutionPlan _plan = null!;
    protected SemanticSpecification _specification = null!;
    protected SemanticSpecificationRun _reference = null!;
    protected SemanticSpecificationRunRecord _stage = null!;

    void Establish()
    {
        // This executor fixture has typed producers and explicit sources; the canonical corpus stays unmodified.
        _model = Compile("""
            module Billing
              feature Payments
                slice Automation Recovery
                  event Approved
                    id String
                  event Claimed
                    id String
                  event Finished
                    id String
                  event OtherObserved
                    id String
                  command Approve
                    id String identifier
                    produces Approved
                      for id
                      id = id
                  command Claim
                    id String identifier
                    produces Claimed
                      for id
                      id = id
                  reaction Claimer
                    when Approved
                      id
                      invokes Claim
                        id = id
                    where id == "payment-a"
                  reaction Other
                    when Approved
                      id
                      produces OtherObserved
                        id = id
                  reaction Finisher
                    when Claimed
                      id
                      produces Finished
                        id = id
                  specification RedeliveryCascade
                    given Approved
                      for "payment-a"
                      id = "payment-a"
                    when redelivered Approved to Claimer
                    then Claimed
                      id = "payment-a"
                    then Finished
                      id = "payment-a"
                  specification FilteredRedelivery
                    given Approved
                      for "ignored"
                      id = "ignored"
                    when redelivered Approved to Claimer
                    then no events
            """);
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
