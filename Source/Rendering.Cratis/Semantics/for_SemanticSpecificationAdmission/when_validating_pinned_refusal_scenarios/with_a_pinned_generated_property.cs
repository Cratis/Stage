// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_pinned_refusal_scenarios;

public class with_a_pinned_generated_property : given.a_specification_in_a_slice
{
    internal const string Source = """
        concept TicketId : Uuid
          generate uuid
        module Work
          feature Processing
            slice Automation FollowUp
              command Start
                sku String identifier
                produces Started
                  for sku
                  sku = sku
              command Complete
                ticket TicketId generated identifier
                sku String
                produces Completed
                  for ticket
                  ticket = ticket
                  sku = sku
              event Started
                sku String
              event Completed
                ticket TicketId
                sku String
              event Failed
                reason String
              reaction Worker
                when Started
                  invokes Complete
                    sku = sku
                    on refused by validation
                      produces Failed
                        reason = $refusal.message
              specification Completing
                given generated Complete.ticket = "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01"
                when append Started
                  for "one"
                  sku = "one"
                then Completed
                  for "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01"
                  ticket = "5a0b7c1e-2d4f-4a6b-8c9d-0e1f2a3b4f01"
                  sku = "one"
        """;

    void Establish() => Prepare(Source);
    void Because()
    {
        Validate();
        Render();
    }

    [Fact] void should_render_the_specification() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_pin_the_generated_value_before_the_command_runs() => _source.ShouldContain("_scenario.PinGeneratedValue(\"ticket\"");
    [Fact] void should_run_the_scenario_typed_by_the_invoked_command() => _source.ShouldContain("CommandScenario<Complete> _scenario");
    [Fact] void should_not_run_an_untyped_scenario() => _source.ShouldNotContain("CommandScenario<object>");
    [Fact] void should_read_the_appended_events_from_the_event_log() => _source.ShouldContain("_facts = [.. persisted.Skip(initial)];");
}
