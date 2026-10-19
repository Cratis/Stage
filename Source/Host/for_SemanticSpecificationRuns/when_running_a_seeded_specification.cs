// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_running_a_seeded_specification : a_specification_endpoint
{
    protected override bool Seeded => true;

    async Task Because() => await Request("{}");

    [Fact] void should_return_ok() => _status.ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_pass_the_seeded_specification() => Report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_not_report_a_refused_capability() => Report.Results.Single().Unsupported.ShouldBeNull();
}
