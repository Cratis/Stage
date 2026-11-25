// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSurfaceLedger;

public class when_classifying_availability_members : Specification
{
    [Fact]
    void should_keep_the_audience_name_rendered() =>
        SemanticSurfaceLedger.Entries["SemanticAudienceRequirement.Audience"].ShouldEqual(new(SemanticSurfaceDispositionKind.Rendered));

    [Fact]
    void should_refuse_negation_until_it_can_be_rendered_faithfully() =>
        SemanticSurfaceLedger.Entries["SemanticAudienceRequirement.Negated"].ShouldEqual(new(SemanticSurfaceDispositionKind.Rejected, SemanticAvailabilityRendering.UnsupportedCondition));

    [Theory]
    [InlineData("SemanticScreenData.Available")]
    [InlineData("SemanticScreenAction.Available")]
    public void should_keep_screen_part_availability_in_the_stack_neutral_ui_contract(string member) =>
        SemanticSurfaceLedger.Entries[member].ShouldEqual(new(SemanticSurfaceDispositionKind.Ignored, "Screens are a stack-neutral UI contract."));
}
