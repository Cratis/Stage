// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_a_newer_minor_within_range : a_plan_at_an_esm_pair
{
    SemanticSpecificationRunReport _newer = null!;
    SemanticSpecificationRunReport _audited = null!;

    async Task Because()
    {
        var plan = WithApplication(application => application, 32, 1);
        _newer = await new SemanticSpecificationExecutor().Run(plan, new([_specification.Id]), new());
        var audited = WithApplication(application => application, 32, 0);
        _audited = await new SemanticSpecificationExecutor().Run(audited, new([_specification.Id]), new());
    }

    [Fact] void should_run_instead_of_refusing_the_version() => _newer.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_preserve_the_audited_pair() => _audited.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
}
