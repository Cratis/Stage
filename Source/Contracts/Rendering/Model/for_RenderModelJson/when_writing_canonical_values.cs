// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.Model.for_RenderModelJson;

public class when_writing_canonical_values : Specification
{
    static string Json<T>(T value) => Encoding.UTF8.GetString(RenderModelJson.Serialize(value));

    [Fact] void should_sort_map_keys_ordinally() => Json(new Dictionary<string, object> { ["z"] = 1, ["a"] = new Dictionary<string, int> { ["ø"] = 3, ["I"] = 2 } }).ShouldEqual("{\"a\":{\"I\":2,\"ø\":3},\"z\":1}");
    [Fact] void should_preserve_semantic_sequence_order() => Json(new RenderSequence<string>(["z", "a"])).ShouldEqual("[\"z\",\"a\"]");
    [Fact] void should_keep_non_ascii_relaxed() => Json("æøå İstanbul <>&").ShouldEqual("\"æøå İstanbul <>&\"");
    [Fact] void should_not_rewrite_literal_backslashes() => Json("\\n\\t").ShouldEqual("\"\\\\n\\\\t\"");

    [Theory]
    [InlineData("0", "0")]
    [InlineData("-1", "-1")]
    [InlineData("9007199254740992", "9007199254740992")]
    [InlineData("12.3400", "12.34")]
    [InlineData("0.0000001", "0.0000001")]
    [InlineData("0.0000000000000000000000000001", "0.0000000000000000000000000001")]
    [InlineData("-0.000", "0")]
    void should_write_shortest_invariant_decimal_forms(string number, string expected) => Json(decimal.Parse(number, CultureInfo.InvariantCulture)).ShouldEqual(expected);

    [Theory]
    [InlineData("tr-TR")]
    [InlineData("nb-NO")]
    void should_be_independent_of_current_culture(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        var expected = RenderModelJson.Serialize(given.render_models.Decimal());
        Encoding.UTF8.GetString(expected).ShouldContain("12.34");
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            given.render_models.EqualBytes(expected, RenderModelJson.Serialize(given.render_models.Decimal()));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Fact]
    void should_hex_escape_every_control_character()
    {
        var controls = new string([.. Enumerable.Range(0, 32).Select(value => (char)value)]);
        var expected = "\"" + string.Concat(Enumerable.Range(0, 32).Select(value => "\\u" + value.ToString("X4", CultureInfo.InvariantCulture))) + "\"";
        Json(controls).ShouldEqual(expected);
    }
}
