// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_validating_multipart_query_specifications : Specification
{
    SemanticExecutionPlan _plan = null!;
    Dictionary<string, SemanticSpecificationRunRecord> _results = [];

    void Establish()
    {
        var model = SemanticModelSerializer.Deserialize(CompositeReadModelKeysCorpus.V27.EsmBytes.AsSpan());
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var invalid = slice.Specifications.Single(specification => specification.Name == "InvalidByPart");
        var request = invalid.ThenQueries.Single();
        var invoice = slice.ReadModels.Single().Properties.Single(property => property.Name == "invoiceId");
        var key = (SemanticCompositeValue)request.Key;
        var valid = invalid with
        {
            Id = slice.Specifications.Single(specification => specification.Name == "FilterOnePart").Id,
            Name = "ValidByPart",
            ThenErrors = [],
            ThenQueries = [request with { Key = key with { Properties = [.. key.Properties.Select(value => value.TargetProperty == invoice.Id ? value with { Value = SemanticValue.Text("INV-1") } : value)] } }]
        };
        var application = model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [invalid, slice.Specifications.Single(specification => specification.Name == "InvalidAssertedByPart"), valid] }] }] }]
        };
        _plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
    }

    async Task Because() => _results = (await new SemanticSpecificationExecutor().Run(_plan, new([]), new())).Results.ToDictionary(result => result.Name);

    [Fact] void should_admit_each_query_specification() => _plan.Specifications.Values.ShouldEachConformTo(specification => SemanticSpecificationAdmission.Check(_plan, specification) is null);
    [Fact] void should_match_the_reference() => _plan.Specifications.Values.ShouldEachConformTo(specification => new SemanticSpecificationRunner().Run(_plan, specification.Id).Passed);
    [Fact] void should_pass_both_invalid_key_specifications_and_the_valid_control() => _results.Values.ShouldEachConformTo(result => result.Outcome == SemanticSpecificationOutcome.Passed);
    [Fact] void should_reject_the_invalid_when_query() => _results["InvalidByPart"].ExecutionKind.ShouldEqual(nameof(SemanticExecutionOutcomeKind.Rejected));
    [Fact] void should_reject_the_invalid_asserted_query() => _results["InvalidAssertedByPart"].ExecutionKind.ShouldEqual(nameof(SemanticExecutionOutcomeKind.Rejected));
    [Fact] void should_preserve_the_exact_validation_message() => _results["InvalidByPart"].Trace!.Rejection.ShouldEqual("Invoice id is too short");
    [Fact] void should_accept_the_valid_control() => _results["ValidByPart"].ExecutionKind.ShouldEqual(nameof(SemanticExecutionOutcomeKind.Accepted));
}
