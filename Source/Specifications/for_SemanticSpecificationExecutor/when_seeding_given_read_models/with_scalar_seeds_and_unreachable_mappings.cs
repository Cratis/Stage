// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_seeding_given_read_models;

public class with_scalar_seeds_and_unreachable_mappings : Specification
{
    SemanticSpecification _specification = null!;
    SemanticSpecificationRunRecord _result = null!;

    async Task Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ScalarSeeds"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", with_keyed_state.Source);
        var compilation = new SemanticModelCompiler().Compile("ScalarSeeds", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value.Model).Plan;
        _specification = plan.Specifications.Values.Single(specification => specification.Name == "QueryingWithoutEvents");
        _result = (await new SemanticSpecificationExecutor().Run(plan, new([_specification.Id]), new())).Results.Single();
    }

    [Fact] void should_have_no_reachable_event_mapping() => _specification.GivenEvents.ShouldBeEmpty();
    [Fact] void should_keep_the_scalar_seed_query_result() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
}
