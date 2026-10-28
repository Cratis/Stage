// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticProjectionProducts;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class and_reading_a_computed_event_member : a_rendered_reaction_body
{
    const string Body = "return new object[] { new LineTotalComputed(@event.UnitPriceTimesQuantity) };";

    void Because() => Render(when_rendering_a_total_that_adds_a_product.Source + "\n" +
        "    slice Automation Notify\n" +
        "      event LineTotalComputed\n" +
        "        total Decimal\n" +
        "      reaction Notifier\n" +
        "        when LineAdded\n" +
        "          ```csharp\n" +
        "          " + Body + "\n" +
        "          ```\n");

    [Fact] void should_publish_and_compile() => ShouldPublishAndCompile(Body);
}
#endif
