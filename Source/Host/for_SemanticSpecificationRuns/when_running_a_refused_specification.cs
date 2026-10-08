// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_running_a_refused_specification : a_specification_endpoint
{
    protected override bool Seeded => true;

    async Task Because() => await Request("{}");

    [Fact] void should_return_ok() => _status.ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_return_unsupported_not_failed() => Report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_identify_the_given_read_model_capability() => Report.Results.Single().Unsupported!.Capability.ShouldEqual(StageExecutionCapability.GivenReadModel);
    [Fact] void should_carry_the_shared_admission_details() => Report.Results.Single().Unsupported!.Details.ShouldEqual(SemanticRunAdmission.Check(_plan, _plan.Specifications.Values.Single())!.Details);
}
