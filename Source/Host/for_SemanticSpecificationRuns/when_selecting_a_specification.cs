// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_selecting_a_specification : a_specification_endpoint
{
    async Task Because() => await Request($$"""{"scopes":["{{_plan.Specifications.Keys.Single()}}","sem1:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"]}""");

    [Fact] void should_run_the_selected_specification() => Report.Results.Single(result => result.SpecificationId == _plan.Specifications.Keys.Single().ToString()).Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_not_drop_the_unknown_scope() => Report.Results.Single(result => result.SpecificationId == "sem1:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa").Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_return_both_records() => Report.Results.Count.ShouldEqual(2);
}
