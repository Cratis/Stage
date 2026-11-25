// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Conformance;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ArcScreenplayRoundTrip.when_comparing_invocation_mappings;

public partial class and_semantic_ids_are_regenerated : Specification
{
    [Fact]
    void should_compare_resolved_structural_targets_instead_of_raw_ids()
    {
        var json = Encoding.UTF8.GetString(ReactionsCorpus.V6.EsmBytes.AsSpan());
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        var regenerated = Identity.Replace(json, match =>
        {
            if (!ids.TryGetValue(match.Value, out var replacement))
            {
                replacement = match.Value[..5] + (ids.Count + 1).ToString("x64", CultureInfo.InvariantCulture);
                ids.Add(match.Value, replacement);
            }
            return replacement;
        });
        var expectedAssignments = Invocations(SemanticModelNormalizer.NormalizeCanonicalJson(Encoding.UTF8.GetBytes(json)).Root).First().Members["mappings"].Items;
        var actualAssignments = Invocations(SemanticModelNormalizer.NormalizeCanonicalJson(Encoding.UTF8.GetBytes(regenerated)).Root).First().Members["mappings"].Items;
        var expected = round_trip_invocation_mappings.Normalize(invocation_mapping_models.Model(expectedAssignments));
        var actual = round_trip_invocation_mappings.Normalize(invocation_mapping_models.Model(actualAssignments.Reverse()));

        Assert.NotEmpty(ids);
        Assert.True(expectedAssignments.Length > 1);
        SemanticModelEquivalence.Compare(expected, actual).IsEquivalent.ShouldBeTrue();
    }

    static IEnumerable<NormalizedSemanticNode> Invocations(NormalizedSemanticNode node)
    {
        if (node.Members.TryGetValue("invokes", out var invokes)) return invokes.Items;
        return node.Members.Values.Concat(node.Items).SelectMany(Invocations);
    }

    [GeneratedRegex("(?:sem1|evt1):[0-9a-f]{64}", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex Identity { get; }
}
#endif
