// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderInventory;

public class when_matching_the_nonempty_json_golden : given.an_inventory
{
    [Fact]
    void should_pin_every_json_byte_with_unicode_and_two_hop_causes()
    {
        var root = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (!File.Exists(Path.Combine(root.FullName, "Stage.slnx"))) root = root.Parent!;
        var path = Path.Combine(root.FullName, "Source", "Contracts", "Rendering", "for_ArtifactRenderInventory", "completeness.golden.json");
        var expected = File.ReadAllText(path, Encoding.UTF8).Replace("$SCOPE$", _request.Scope.Artifact.ToString(), StringComparison.Ordinal);
        Assert.Equal(Encoding.UTF8.GetBytes(expected), ArtifactRenderReports.Create(_plan, _context).Completeness.Bytes.ToArray());
    }

    [Fact]
    void should_not_allow_report_provenance_to_relabel_the_input()
    {
        var other = new ArtifactRenderVersionPair(LanguageVersion.V2, SemanticVersion.V2);
        var context = _context with { Input = other, Support = new([_context.Input, other]) };
        Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderReports.Create(_plan, context));
    }

    [Fact]
    void should_preserve_bytes_when_exact_audited_pairs_and_pack_pins_are_reordered()
    {
        var other = new ArtifactRenderVersionPair(LanguageVersion.V2, SemanticVersion.V2);
        var context = _context with { Support = new([other, _context.Input]), Packs = [new("Z.Pack", "2.0.0"), new("A.Pack", "1.0.0")] };
        var reversed = context with { Support = new([.. context.Support.AuditedPairs.Reverse()]), Packs = [.. context.Packs.Reverse()] };
        Assert.True(ArtifactRenderReports.Create(_plan, context).Completeness.Bytes.SequenceEqual(ArtifactRenderReports.Create(_plan, reversed).Completeness.Bytes));
    }

    [Fact]
    void should_hash_exact_target_version_provenance()
    {
        var profile = ArtifactRenderProfile.Create(_plan.Target, "2", _plan.Renderer, _plan.RendererVersion, []);
        var changed = ArtifactRenderPlan.Create(_request with { Profile = profile }, [], _plan.Diagnostics).WithInventory(_inventory);
        Assert.NotEqual(ArtifactRenderReports.Create(_plan, _context).Completeness.Sha256, ArtifactRenderReports.Create(changed, _context).Completeness.Sha256);
    }
}
