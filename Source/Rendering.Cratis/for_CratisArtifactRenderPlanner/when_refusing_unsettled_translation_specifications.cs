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
        Prepare(Source + "\n      capture OtherIntake\n        source events\n          from Shipment\n        key id\n        append Received\n          id = $.id\n");
        Refuses("exactly one");
    }

    [Fact]
    void should_not_bypass_append_time_constraints_on_the_produced_fact()
    {
        Prepare(Source);
        _model = Transform(_model, slice => slice with
        {
            Constraints = [new("UniqueId", SemanticConstraintKind.UniquePropertyValue, SemanticConstraintScope.EventSequence, [new(slice.Events.Single(contract => contract.Name == "Received").Id, [slice.Events.Single(contract => contract.Name == "Received").Properties.Single().Id])], [], false, null)]
        });
        Refuses("append-time constraints");
    }

    // A specification runs only its own slice (Screenplay decision 0115): another slice's publication of the output does not run.
    [Fact]
    void should_not_settle_another_slices_publication_of_the_output()
    {
        Prepare(Source + "\n" + Publication);
        _plan = Plan(_model);
        Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    }

    [Fact]
    void should_refuse_a_publication_named_after_the_generated_failure_type()
    {
        Prepare(Source + "\n" + Publication.Replace("projection ReceiptPublisher", "projection PublicationAppendFailed", StringComparison.Ordinal));
        _plan = Plan(_model);
        given.rendering_refusals.Rejected(_plan, "STAGE-ESM-012");
        _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Message.Contains("append failure", StringComparison.Ordinal));
    }

    void Prepare(string source) => _model = Semantics.for_SemanticAutomationArtifactRenderer.given.an_automation.Compile(source);

    void Refuses(string reason)
    {
        _plan = Plan(_model);
        given.rendering_refusals.Rejected(_plan, "STAGE-ESM-011");
        _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains(reason, StringComparison.Ordinal));
    }
}
#endif
