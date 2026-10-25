// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_an_expected_rejection_feeds_an_unadmittable_projection : given.a_universal_projection
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_settle_the_outcome_without_lowering_the_projection(bool rejectedByRule)
    {
        _plan = CreatePlan(true, mapping: "\"fixed\"", expectRejection: true, rejectCommand: rejectedByRule);
        await Run();
        _reference.Passed.ShouldEqual(rejectedByRule);
        _result.Outcome.ShouldEqual(rejectedByRule ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed);
        _result.Unsupported.ShouldBeNull();
        _result.Failures.ShouldContainOnly(_reference.Failures);
        _result.Trace!.ReadModels.ShouldBeEmpty();
        if (rejectedByRule)
        {
            _result.Failures.ShouldBeEmpty();
            _result.Trace.Rejection.ShouldEqual("Description is required");
        }
        else
        {
            _result.Failures.ShouldContainOnly("Expected Rejected, got Accepted.");
            _result.Trace.Rejection.ShouldBeNull();
        }
    }
}
