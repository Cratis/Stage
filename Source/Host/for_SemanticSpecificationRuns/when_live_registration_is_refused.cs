// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_live_registration_is_refused : a_specification_endpoint
{
    protected override bool LiveRegistrationRefused => true;

    async Task Because() => await Request("{}");

    [Fact] void should_keep_isolated_specifications_available() => _status.ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_use_the_admitted_model_not_the_live_world() => Report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
}
