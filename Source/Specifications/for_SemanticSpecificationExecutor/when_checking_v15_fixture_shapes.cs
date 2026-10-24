// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_checking_v15_fixture_shapes : Specification
{
    [Theory]
    [InlineData("Uuid", "", "11111111-1111-1111-1111-111111111111")]
    [InlineData("String", "  generate uuid", "00112233-4455-4677-8899-AABBCCDDEEFF")]
    [InlineData("String", "  generate bytes 16 base32", "AAAAAAAAAAAAAAAAAAAAAAAAAB")]
    [InlineData("String", "  generate bytes 16 base64url", "AAAAAAAAAAAAAAAAAAAAAB")]
    public async Task should_reject_bad_strategy_shapes_like_the_reference(string primitive, string generation, string fixture)
    {
        var plan = a_generation_plan.Compile(primitive, generation, fixture, "        then error \"Generated fixture for 'id' does not match its generation strategy shape.\"");
        var specification = plan.Specifications.Values.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        reference.Passed.ShouldBeTrue();
        ((SemanticRejected)reference.Execution).Category.ShouldEqual(SemanticRejectionCategory.Contract);
        var stage = (await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken)).Results.Single();
        stage.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
        stage.Trace.Rejection.ShouldEqual(((SemanticRejected)reference.Execution).Details);
        stage.Trace.Facts.ShouldBeEmpty();
    }
}
