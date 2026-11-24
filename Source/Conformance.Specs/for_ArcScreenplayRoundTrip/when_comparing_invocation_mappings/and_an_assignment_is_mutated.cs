// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Conformance;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.when_comparing_invocation_mappings;

public class and_an_assignment_is_mutated : Specification
{
    [Theory]
    [InlineData("changed")]
    [InlineData("missing")]
    [InlineData("extra")]
    [InlineData("swapped")]
    [InlineData("root")]
    [InlineData("source kind")]
    [InlineData("target")]
    void should_report_the_structural_target_not_an_ordinal(string mutation)
    {
        var assignments = invocation_mapping_models.Assignments;
        var actual = (NormalizedSemanticNode[])(mutation switch
        {
            "changed" => [assignments[0], invocation_mapping_models.Assignment(invocation_mapping_models.SecondTarget, invocation_mapping_models.FirstTarget)],
            "missing" => [assignments[0]],
            "swapped" => [invocation_mapping_models.Assignment(invocation_mapping_models.FirstTarget, invocation_mapping_models.SecondTarget), invocation_mapping_models.Assignment(invocation_mapping_models.SecondTarget, invocation_mapping_models.FirstTarget)],
            "root" => [assignments[0], invocation_mapping_models.Assignment(invocation_mapping_models.SecondTarget, invocation_mapping_models.SecondTarget, root: "command")],
            "source kind" => [assignments[0], invocation_mapping_models.Assignment(invocation_mapping_models.SecondTarget, invocation_mapping_models.SecondTarget, sourceKind: "concept")],
            "target" => [assignments[0], invocation_mapping_models.Assignment(invocation_mapping_models.SecondTarget + "Different", invocation_mapping_models.SecondTarget)],
            _ => [.. assignments, invocation_mapping_models.Assignment(invocation_mapping_models.FirstTarget + "Extra", invocation_mapping_models.FirstTarget)]
        });
        var comparison = SemanticModelEquivalence.Compare(
            round_trip_invocation_mappings.Normalize(invocation_mapping_models.Model(assignments)),
            round_trip_invocation_mappings.Normalize(invocation_mapping_models.Model(actual)));

        comparison.IsEquivalent.ShouldBeFalse();
        Assert.All(comparison.Differences, difference => Assert.Contains("/mappings[/application/", difference.Path, StringComparison.Ordinal));
    }

    [Fact]
    void should_preserve_the_complete_literal_source()
    {
        var expected = invocation_mapping_models.Model([invocation_mapping_models.LiteralAssignment("original")]);
        var actual = invocation_mapping_models.Model([invocation_mapping_models.LiteralAssignment("changed")]);
        SemanticModelEquivalence.Compare(round_trip_invocation_mappings.Normalize(expected), round_trip_invocation_mappings.Normalize(actual)).IsEquivalent.ShouldBeFalse();
    }

    [Fact]
    void should_preserve_source_path_order()
    {
        var expected = invocation_mapping_models.Model([invocation_mapping_models.PathAssignment(reverse: false)]);
        var actual = invocation_mapping_models.Model([invocation_mapping_models.PathAssignment(reverse: true)]);
        SemanticModelEquivalence.Compare(round_trip_invocation_mappings.Normalize(expected), round_trip_invocation_mappings.Normalize(actual)).IsEquivalent.ShouldBeFalse();
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("unresolved")]
    [InlineData("missing target")]
    void should_reject_an_ambiguous_or_unresolved_target(string mutation)
    {
        var target = mutation == "duplicate" ? invocation_mapping_models.FirstTarget : "unresolved-id";
        var assignment = mutation == "missing target"
            ? invocation_mapping_models.Object(("source", NormalizedSemanticNode.Leaf("null")))
            : invocation_mapping_models.Assignment(target, invocation_mapping_models.SecondTarget);
        var assignments = new[] { invocation_mapping_models.Assignments[0], assignment };

        Assert.Throws<SemanticNormalizationFailed>(() => round_trip_invocation_mappings.Normalize(invocation_mapping_models.Model(assignments)));
    }
}
#endif
