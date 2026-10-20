// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_append_refusals;

public class with_an_append_a_unique_event_constraint_refuses : given.a_specification_in_a_slice
{
    internal const string Source = """
        module Work
          feature Processing
            slice StateChange Dispatch
              command Dispatch
                id String identifier
                carrier String
                produces Dispatched
                  for id
                  id = id
                  carrier = carrier
              event Dispatched
                id String
                carrier String
              constraint OneDispatch
                unique event Dispatched
                message "Dispatched once"
              specification DispatchedOnce
                given Dispatched
                  for "one"
                  id = "one"
                  carrier = "post"
                when append Dispatched
                  for "one"
                  id = "one"
                  carrier = "other"
                then error "Dispatched once"
        """;

    void Establish() => Prepare(Source);
    void Because()
    {
        Validate();
        Render();
    }

    [Fact] void should_render_the_specification() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_append_through_the_event_log() => _source.ShouldContain("_scenario.EventLog.Append(");
    [Fact] void should_assert_the_append_failed() => _source.ShouldContain("_result.IsSuccess.ShouldBeFalse()");
    [Fact] void should_assert_the_constraint_by_name() => _source.ShouldContain("violation.ConstraintName == \"OneDispatch\"");
    [Fact] void should_assert_the_constraint_message() => _source.ShouldContain("violation.Message == \"Dispatched once\"");
    [Fact] void should_assert_that_nothing_else_was_appended() => _source.ShouldContain("should_not_append_events");
}
