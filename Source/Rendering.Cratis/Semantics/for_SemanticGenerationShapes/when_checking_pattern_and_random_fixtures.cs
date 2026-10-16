// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticGenerationShapes;

public class when_checking_pattern_and_random_fixtures : Specification
{
    [Theory]
    [InlineData("#{2}", "luhn", "009", true)]
    [InlineData("#{2}", "luhn", "09", false)]
    [InlineData("#{2}", "mod97", "0099", true)]
    [InlineData("#{2}", "mod11", "00A", false)]
    [InlineData("[A-C]{1,3}#", null, "ABC9", true)]
    [InlineData("[A-C]{1,3}#", null, "ABCD9", false)]
    [InlineData("[A-C]{1,3}#", null, "ABC9\n", false)]
    [InlineData("[\\]\\-\\\\]#", null, "]9", true)]
    [InlineData("[\\]\\-\\\\]#", null, "-9", true)]
    [InlineData("[\\]\\-\\\\]#", null, "\\9", true)]
    [InlineData("[\\]\\-\\\\]#", null, "A9", false)]
    [InlineData("\\{yyyy\\}\\#?*", null, "{yyyy}#AZ", true)]
    [InlineData("{MM}{dd}-{MM}{dd}-?", null, "9999-9999-A", true)]
    public void should_match_the_exact_finite_language(string pattern, string? checksum, string value, bool expected)
    {
        var concept = a_pattern_random_model.Model().Application.Concepts[0] with { Generation = new("pattern") { Pattern = pattern, Checksum = checksum } };
        SemanticGenerationShapes.Matches(concept, SemanticValue.Text(value)).ShouldEqual(expected);
    }

    [Theory]
    [InlineData("0229-0229-A", true)]
    [InlineData("0301-0301-A", false)]
    [InlineData("0229-0301-A", false)]
    public void should_fix_every_date_segment_to_the_occurrence_utc_date(string value, bool expected)
    {
        var concept = a_pattern_random_model.Model().Application.Concepts[0] with { Generation = new("pattern") { Pattern = "{MM}{dd}-{MM}{dd}-?" } };
        SemanticGenerationShapes.Matches(concept, SemanticValue.Text(value), new DateTimeOffset(2024, 3, 1, 0, 30, 0, TimeSpan.FromHours(2))).ShouldEqual(expected);
    }

    [Theory]
    [InlineData(-2, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(-3, false)]
    [InlineData(0.5, false)]
    public void should_check_inclusive_integral_random_bounds(double value, bool expected)
    {
        var concept = a_pattern_random_model.Model().Application.Concepts.Single(concept => concept.Name == "Ticket") with { Generation = new("random") { Minimum = -2, Maximum = 2 } };
        SemanticGenerationShapes.Matches(concept, SemanticValue.Number((decimal)value)).ShouldEqual(expected);
        SemanticGenerationShapes.Matches(concept, SemanticValue.Text(value.ToString(System.Globalization.CultureInfo.InvariantCulture))).ShouldBeFalse();
    }
}
