// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_a_projection_lowering_fails : a_projected_specification
{
    [Theory]
    [InlineData("accepted")]
    [InlineData("rejected")]
    [InlineData("denied")]
    public async Task should_classify_the_failure_without_overriding_an_earlier_rejection(string scenario)
    {
        var specification = scenario switch
        {
            "accepted" => """
                specification ProjectionFailure
                  when RegisterInvoice
                    invoiceId = "first"
                    description = "North"
                  then InvoiceRegistered
                    for "first"
                    description = "North"
                  then readmodel InvoiceView
                    invoiceId = "first"
                    description = "North"
                """,
            "rejected" => """
                specification InvalidCommand
                  when RegisterInvoice
                    invoiceId = "first"
                    description = ""
                  then error "Description is required"
                """,
            _ => """
                specification DeniedQuery
                  given caller
                    authenticated
                  given InvoiceRegistered
                    for "first"
                    description = "North"
                  when query InvoiceById
                    invoiceId = "first"
                  then denied
                """
        };
        _plan = Compile(specification, commandScenario: scenario != "denied", validateDescription: true);
        var reference = new SemanticSpecificationRunner().Run(_plan, _plan.Specifications.Keys.Single());
        reference.Passed.ShouldBeTrue();
        var cache = (ConcurrentDictionary<SemanticRevision, SemanticRuntimeTypes>)typeof(SemanticSpecificationExecutor)
            .GetField("_types", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var types = new SemanticRuntimeTypes(_plan);
        var models = (Dictionary<SemanticId, Type>)typeof(SemanticRuntimeTypes)
            .GetField("_readModels", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(types)!;

        // Inject a broken emitted read-model type so the real projection builder fails if it is reached.
        models.Add(_plan.ReadModels.Keys.Single(), typeof(object));
        cache.TryAdd(_plan.Revision, types).ShouldBeTrue();
        try
        {
            _result = Assert.Single((await new SemanticSpecificationExecutor().Run(_plan, new([]), new())).Results);
            if (scenario == "accepted")
            {
                _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
                _result.ExecutionKind.ShouldEqual("Rejected");
                _result.Trace!.RejectionCode.ShouldEqual("LoweringRefused");
                Assert.StartsWith("Expected Accepted, got Rejected.", Assert.Single(_result.Failures));
            }
            else
            {
                _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
                _result.Failures.ShouldBeEmpty();
                Assert.NotEqual("LoweringRefused", _result.Trace!.RejectionCode);
                _result.Trace.ReadModels.ShouldBeEmpty();
                _result.Trace.Queries.ShouldBeEmpty();
            }
        }
        finally
        {
            cache.TryRemove(_plan.Revision, out _);
        }
    }
}
