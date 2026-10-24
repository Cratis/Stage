// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_unsettled_translation_specifications : given.a_translation_application
{
    const string Source = """
        module Shipping
          feature Fulfilment
            slice Translate ReceiveShipment
              direction inbound
              event Received
                id String
              event Shipment from "Carrier"
                id String
              capture ShipmentIntake
                source events
                  from Shipment
                key id
                append Received
                  id = $.id
              specification ReceivingAShipment
                when append Shipment
                  id = "one"
                then Received
                  for "one"
                  id = "one"
        """;

    const string Publication = """

            slice Translate PublishReceipt
              direction outbound
              public event ReceiptPublished
                id String
              projection ReceiptPublisher => ReceiptPublished
                from Received
                  id = id
        """;

    [Fact]
    void should_not_ignore_another_capture_of_the_same_input()
    {
        var model = Compile(Source + "\n      capture OtherIntake\n        source events\n          from Shipment\n        key id\n        append Received\n          id = $.id\n");
        Refuses(model, "exactly one");
    }

    [Fact]
    void should_not_bypass_append_time_constraints_on_the_produced_fact()
    {
        var model = Transform(Compile(Source), slice => slice with
        {
            Constraints = [new("UniqueId", SemanticConstraintKind.UniquePropertyValue, SemanticConstraintScope.EventSequence, [new(slice.Events.Single(contract => contract.Name == "Received").Id, [slice.Events.Single(contract => contract.Name == "Received").Properties.Single().Id])], [], false, null)]
        });
        Refuses(model, "append-time constraints");
    }

    [Fact]
    void should_not_ignore_a_publication_triggered_by_the_output()
    {
        var model = Compile(Source + "\n" + Publication);
        Refuses(model, "cascade");
    }

    [Fact]
    void should_refuse_a_publication_named_after_the_generated_failure_type()
    {
        var model = Compile(Source + "\n" + Publication.Replace("projection ReceiptPublisher", "projection PublicationAppendFailed", StringComparison.Ordinal));
        var plan = Plan(model);
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Message.Contains("append failure", StringComparison.Ordinal));
    }

    static ExecutableSemanticModel Compile(string source) => Semantics.for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(source);

    static void Refuses(ExecutableSemanticModel model, string reason)
    {
        var plan = Plan(model);
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains(reason, StringComparison.Ordinal));
    }
}
#endif
