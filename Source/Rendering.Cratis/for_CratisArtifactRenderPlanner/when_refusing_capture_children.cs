// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_capture_children : given.a_translation_source
{
    const string Source = """
        type Line
          sku String
          quantity Int
        module Shop
          feature Sales
            slice Translate Receive
              direction inbound
              event OrderConfirmed from "Sales"
                orderId String
                lines Line[]
              capture Demand
                source events
                  from OrderConfirmed
                key orderId
                children lines identified by sku
                  append DemandRecorded
                    when added
                      orderId = $.orderId
                      sku = $.sku
                      quantity = $.quantity
              event DemandRecorded
                orderId String
                sku String
                quantity Int
        """;

    void Establish() => _model = Compile(Source);
    void Because() => _plan = Plan(_model);

    [Fact] void should_refuse_instead_of_silently_dropping_the_child_appends() => given.rendering_refusals.Rejected(_plan, "STAGE-ESM-033");
    [Fact] void should_emit_no_incomplete_capture() => given.rendering_refusals.OmittedParts(_plan);
    [Fact] void should_identify_the_refused_capture() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-033" && diagnostic.Message.Contains("Demand", StringComparison.Ordinal));
    [Fact] void should_report_the_unsupported_translation_shape() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-024");
}
#endif
