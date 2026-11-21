// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_running_a_healthy_specification_beside_a_refused_one : a_specification_endpoint
{
    protected override string Source => for_SemanticHost.given.a_refused_specification.Source;

    string HealthyId => _loaded.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().Id.ToString();

    async Task Because() => await Request($$"""{"scopes":["{{HealthyId}}"]}""");

    [Fact] void should_return_ok() => _status.ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_run_the_healthy_specification() => Report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_not_silently_run_the_refused_specification() => Report.Results.Single().SpecificationId.ShouldEqual(HealthyId);
}
