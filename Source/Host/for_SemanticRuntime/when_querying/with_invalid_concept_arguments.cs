// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_querying;

public class with_invalid_concept_arguments : Specification
{
    SemanticExecutionPlan _plan = null!;
    ISemanticRuntime _runtime = null!;
    readonly Dictionary<string, SemanticExecutionResult> _results = [];

    void Establish()
    {
        _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(QueryValidationCorpus.V18.EsmBytes.AsSpan())).Plan!;
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        _runtime = SemanticRuntimeHosting.Create(_plan, appender);
    }

    async Task Because()
    {
        foreach (var specification in _plan.Specifications.Values)
        {
            var request = specification.ThenQueries.Single();
            _results[specification.Name] = await _runtime.Query(_plan.Queries[request.Query], request.Key, new ClaimsPrincipal(), request.Filters);
        }
    }

    [Fact] void should_execute_every_corpus_request() => _results.Count.ShouldEqual(6);
    [Fact] void should_reject_invalid_keys_and_filters_with_the_validation_category() => _results.Where(result => result.Key.StartsWith("Invalid", StringComparison.Ordinal)).All(result => result.Value is SemanticRejected { Category: SemanticRejectionCategory.Validation }).ShouldBeTrue();
    [Fact] void should_preserve_the_key_message() => ((SemanticRejected)_results["InvalidKey"]).ValidationFailures.Single().Message.ShouldEqual("Invoice number is too short");
    [Fact] void should_preserve_the_filter_message() => ((SemanticRejected)_results["InvalidFilter"]).ValidationFailures.Single().Message.ShouldEqual("Customer code is too short");
    [Fact] void should_accept_valid_unmatched_keys_and_omitted_optional_filters() => _results.Where(result => !result.Key.StartsWith("Invalid", StringComparison.Ordinal)).All(result => result.Value is SemanticAccepted { Queries: [{ Results.IsEmpty: true }] }).ShouldBeTrue();
}
