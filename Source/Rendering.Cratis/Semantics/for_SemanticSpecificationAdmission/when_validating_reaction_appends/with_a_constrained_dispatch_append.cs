// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_a_constrained_dispatch_append : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish() => Prepare(ProductionSource
        .Replace("Started", "ShipmentDispatched", StringComparison.Ordinal)
        .Replace("Following", "AShipmentIsDispatchedOnce", StringComparison.Ordinal) +
        "\n      constraint OneDispatchPerShipment\n        unique event ShipmentDispatched\n");
    void Because() => Validate();
    [Fact] void should_name_the_constrained_append() => _diagnostics.Single().Message.ShouldContain("Appended event 'ShipmentDispatched' participates in append-time constraint(s) 'OneDispatchPerShipment'");
    [Fact] void should_require_an_append_preserving_harness() => _diagnostics.Single().Message.ShouldContain("event-log append and settlement harness");
    [Fact] void should_locate_the_refusal_on_the_specification() => _diagnostics.Single().Artifact.ShouldEqual(_specification.Id);
}
