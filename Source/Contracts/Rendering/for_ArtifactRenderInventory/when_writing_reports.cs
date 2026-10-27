// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderInventory;

public class when_writing_reports : given.an_inventory
{
    [Fact]
    void should_pin_schema_scope_score_and_canonical_outcome()
    {
        using var json = JsonDocument.Parse(ArtifactRenderReports.Create(_plan, _context).Completeness.Bytes.AsMemory());
        var root = json.RootElement;
        root.GetProperty("schemaVersion").GetString().ShouldEqual("stage-render-completeness/v1");
        root.GetProperty("scoreVersion").GetString().ShouldEqual("stage-render-completeness-score/v1");
        root.GetProperty("outcome").GetString().ShouldEqual("Degraded");
        root.GetProperty("scopes")[0].GetProperty("kind").GetString().ShouldEqual("Application");
        root.GetProperty("scopes")[0].GetProperty("identity").GetString().ShouldEqual(_request.Scope.Artifact.ToString());
        root.GetProperty("inventory").GetProperty("score").GetProperty("basisPoints").GetInt32().ShouldEqual(2142);
        root.GetProperty("support").GetProperty("policy").GetString().ShouldEqual("AuditedOnly");
        root.GetProperty("support").TryGetProperty("forwardInterval", out _).ShouldBeFalse();
    }

    [Fact]
    void should_pin_the_entire_weight_table()
    {
        var weights = string.Join(',', Enum.GetValues<ArtifactRenderConstructKind>().Select(kind => $"{kind}:{ArtifactRenderConstructWeights.For(kind)}"));
        weights.ShouldEqual("Unknown:5,Command:5,Operation:5,Guard:5,Validation:5,Policy:5,Event:3,ReadModel:3,Concept:3,CompositeType:3,Query:3,Projection:3,Reducer:3,Reaction:3,Capture:3,Constraint:3,Screen:1,Specification:1");
    }

    [Fact]
    void should_have_identical_bytes_and_digest_for_reordered_inventory()
    {
        var reordered = Plan(ArtifactRenderInventory.Create([.. _inventory.Constructs.Reverse()]));
        var expected = ArtifactRenderReports.Create(_plan, _context);
        var actual = ArtifactRenderReports.Create(reordered, _context);
        actual.Completeness.Bytes.SequenceEqual(expected.Completeness.Bytes).ShouldBeTrue();
        actual.Unsupported.Bytes.SequenceEqual(expected.Unsupported.Bytes).ShouldBeTrue();
        reordered.Digest.ShouldEqual(_plan.Digest);
    }

    [Fact]
    void should_preserve_unicode_and_emit_utf8_lf_with_one_terminal_newline()
    {
        foreach (var artifact in new[] { ArtifactRenderReports.Create(_plan, _context).Completeness, ArtifactRenderReports.Create(_plan, _context).Unsupported })
        {
            var text = Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
            text.Contains("Café", StringComparison.Ordinal).ShouldBeTrue();
            text.Contains("e\u0301", StringComparison.Ordinal).ShouldBeTrue();
            text.Contains('\r').ShouldBeFalse();
            text.EndsWith('\n').ShouldBeTrue();
            text.EndsWith("\n\n", StringComparison.Ordinal).ShouldBeFalse();
            artifact.Bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble).ShouldBeFalse();
        }
    }

    [Fact]
    void should_pin_an_empty_markdown_report_without_claiming_whole_application_coverage()
    {
        var plan = Plan(ArtifactRenderInventory.Create([]));
        var text = Encoding.UTF8.GetString(ArtifactRenderReports.Create(plan, _context).Unsupported.Bytes.AsSpan());
        text.ShouldEqual($"# Unsupported constructs\n\nSelected scopes:\n- Application `{_request.Scope.Artifact}`\n\nOutcome: Rendered. Owners: 0. Score: not applicable.\n\nNo degraded or refused constructs in this selection.\n");
        using var json = JsonDocument.Parse(ArtifactRenderReports.Create(plan, _context).Completeness.Bytes.AsMemory());
        json.RootElement.GetProperty("inventory").GetProperty("score").GetProperty("basisPoints").ValueKind.ShouldEqual(JsonValueKind.Null);
    }

    [Fact]
    void should_not_include_a_recursive_plan_hash()
    {
        var reports = ArtifactRenderReports.Create(_plan, _context);
        var withReports = ArtifactRenderPlan.Create(_request, [reports.Completeness, reports.Unsupported], _plan.Diagnostics).WithInventory(_inventory);
        ArtifactRenderReports.Create(withReports, _context).Completeness.Bytes.SequenceEqual(reports.Completeness.Bytes).ShouldBeTrue();
        Encoding.UTF8.GetString(reports.Completeness.Bytes.AsSpan()).Contains(_plan.Digest, StringComparison.Ordinal).ShouldBeFalse();
        withReports.Digest.ShouldNotEqual(_plan.Digest);
    }

    [Fact]
    void should_change_report_hashes_when_packs_or_renderer_versions_change()
    {
        var reports = ArtifactRenderReports.Create(_plan, _context);
        ArtifactRenderReports.Create(_plan, _context with { Packs = [new("Example.Pack", "1.2.4")] }).Completeness.Sha256.ShouldNotEqual(reports.Completeness.Sha256);
        var profile = ArtifactRenderProfile.Create(_plan.Target, _plan.TargetVersion, _plan.Renderer, "2", []);
        var changed = ArtifactRenderPlan.Create(_request with { Profile = profile }, [], _plan.Diagnostics).WithInventory(_inventory);
        ArtifactRenderReports.Create(changed, _context).Completeness.Sha256.ShouldNotEqual(reports.Completeness.Sha256);
    }

    [Fact]
    void should_hash_semantic_path_placeholder_and_intent()
    {
        var changed = _root with { Degradations = [_root.Degradations.Single() with { Intent = "Different intent." }] };
        Plan(ArtifactRenderInventory.Create([changed, _middle, _leaf, _rendered])).Digest.ShouldNotEqual(_plan.Digest);
        var path = _rendered with { ModelPath = "model/other.play" };
        Plan(ArtifactRenderInventory.Create([_root, _middle, _leaf, path])).Digest.ShouldNotEqual(_plan.Digest);
        var placeholder = _root with { Degradations = [_root.Degradations.Single() with { PlaceholderKind = ArtifactRenderPlaceholderKind.RetainedSignatureOrType }] };
        Plan(ArtifactRenderInventory.Create([placeholder, _middle, _leaf, _rendered])).Digest.ShouldNotEqual(_plan.Digest);
    }

    [Fact]
    void should_hash_cause_edges_independently_of_owner_counts_and_warning_evidence()
    {
        var direct = _leaf with { Degradations = [_leaf.Degradations.Single() with { Roots = [new(_root.Identity, "STAGE-DEGRADE-002")] }] };
        var plan = Plan(ArtifactRenderInventory.Create([_root, _middle, direct, _rendered]));
        plan.Inventory!.Score.ShouldEqual(_inventory.Score);
        plan.Diagnostics.SequenceEqual(_plan.Diagnostics).ShouldBeTrue();
        plan.Inventory.Digest.ShouldNotEqual(_inventory.Digest);
        plan.Digest.ShouldNotEqual(_plan.Digest);
        var reports = ArtifactRenderReports.Create(plan, _context);
        var original = ArtifactRenderReports.Create(_plan, _context);
        reports.Completeness.Sha256.ShouldNotEqual(original.Completeness.Sha256);
        reports.Unsupported.Sha256.ShouldNotEqual(original.Unsupported.Sha256);
    }

    [Fact]
    void should_normalize_independent_gap_and_cause_order_without_changing_bytes_or_digests()
    {
        var independent = Degraded(1, ArtifactRenderConstructKind.Command, "STAGE-DEGRADE-001", []).Degradations.Single();
        var root = _root with { Degradations = [.. _root.Degradations, independent] };
        var middle = _middle with { Degradations = [_middle.Degradations.Single() with { Roots = [new(root.Identity, "STAGE-DEGRADE-002"), new(root.Identity, "STAGE-DEGRADE-001")] }] };
        var first = Plan(ArtifactRenderInventory.Create([root, middle, _leaf, _rendered]));
        var reversedRoot = root with { Degradations = [.. root.Degradations.Reverse()] };
        var reversedMiddle = middle with { Degradations = [middle.Degradations.Single() with { Roots = [.. middle.Degradations.Single().Roots.Reverse()] }] };
        var second = Plan(ArtifactRenderInventory.Create([_rendered, _leaf, reversedMiddle, reversedRoot]));
        first.Inventory!.Score.Counts.Total.ShouldEqual(4);
        first.Inventory.Digest.ShouldEqual(second.Inventory!.Digest);
        first.Digest.ShouldEqual(second.Digest);
        var expected = ArtifactRenderReports.Create(first, _context);
        var actual = ArtifactRenderReports.Create(second, _context);
        actual.Completeness.Bytes.SequenceEqual(expected.Completeness.Bytes).ShouldBeTrue();
        actual.Unsupported.Bytes.SequenceEqual(expected.Unsupported.Bytes).ShouldBeTrue();
    }

    [Fact]
    void should_reject_missing_audited_input_or_duplicate_pack_pins()
    {
        Catch.Exception(() => ArtifactRenderReports.Create(_plan, _context with { Support = new([]) })).ShouldBeOfExactType<InvalidArtifactRenderContract>();
        Catch.Exception(() => ArtifactRenderReports.Create(_plan, _context with { Packs = [new("X", "1"), new("X", "2")] })).ShouldBeOfExactType<InvalidArtifactRenderContract>();
    }
}
