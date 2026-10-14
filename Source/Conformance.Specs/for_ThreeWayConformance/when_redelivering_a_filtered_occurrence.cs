// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_redelivering_a_filtered_occurrence : Specification
{
    [Theory]
    [InlineData("stream Account.Archive")]
    [InlineData("stream Other.Notes")]
    [InlineData("")]
    public async Task should_produce_no_new_events_without_delivering_to_a_sibling(string route)
    {
        var model = a_recovery_plan.Compile($"""
            eventsource Account
              id "account"
              identifier String
              stream Notes
                id "notes"
              stream Archive
                id "archive"
            eventsource Other
              id "other"
              identifier String
              stream Notes
                id "notes"
            module Work
              feature Items
                slice Automation Recovery
                  command Begin
                    id String identifier
                    produces Started
                      for id
                      id = id
                  event Started
                    id String
                  event Followed
                    id String
                  reaction Filtered
                    from Account.Notes
                    when Started
                      produces Followed
                        for id
                        id = id
                  reaction Sibling
                    when Started
                      produces Followed
                        for id
                        id = "sibling"
                  specification Excluded
                    given Started
                      for "one"
                      {route}
                      id = "one"
                    when redelivered Started to Filtered
                    then no events
            """);
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var specification = plan.Specifications.Values.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        var stage = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
        Assert.True(reference.Passed, string.Join(';', reference.Failures));
        Assert.True(stage.Outcome == SemanticSpecificationOutcome.Passed, $"{stage.Unsupported?.Details} {string.Join(';', stage.Failures)}");
        stage.Trace!.Facts.ShouldBeEmpty();
        reference.Execution.World.Facts.Length.ShouldEqual(1);
    }
}
#endif
