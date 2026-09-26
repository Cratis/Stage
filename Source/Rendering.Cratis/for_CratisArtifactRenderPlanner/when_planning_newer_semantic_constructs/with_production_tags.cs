// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

// Tags travel with every produced fact; an application that drops them appends different events.
public class with_production_tags : given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace(
        "          for streamReference\n",
        "          for streamReference\n          tag \"billing\"\n",
        StringComparison.Ordinal));

    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_render_tags_on_the_event_wrapper() => Artifact("Issue.cs").ShouldContain("Tags = [\"billing\"]");
}
