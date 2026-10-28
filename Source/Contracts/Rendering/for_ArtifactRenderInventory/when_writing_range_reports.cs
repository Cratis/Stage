// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderInventory;

public class when_writing_range_reports : given.an_inventory
{
    ArtifactRenderDegradation _evidence = null!;
    ArtifactRenderPlan _withEvidence = null!;
    ArtifactRenderReportContext _ranged = null!;

    void Establish()
    {
        var warning = new ArtifactRenderDiagnostic(ArtifactRenderDegradationCodes.NewerEsmWithinDeclaredRange, ArtifactRenderDiagnosticSeverity.Warning, "Newer within range.", Id(9));
        _evidence = new(warning, "Upgrade Stage to audit this pair.");
        _withEvidence = ArtifactRenderPlan.Create(
            _request,
            [],
            [.. _inventory.Constructs.SelectMany(construct => construct.Degradations.Select(gap => gap.Evidence.Diagnostic)), warning]).WithInventory(_inventory).WithPlanEvidence([_evidence]);
        var pair = new ArtifactRenderVersionPair(LanguageVersion.V1, SemanticVersion.V1);
        _ranged = new(pair, new([pair]) { DeclaredRange = new(new(LanguageVersion.V32, SemanticVersion.V32), 33) }, [new("Example.Pack", "1.2.3")]);
    }

    [Fact]
    void should_write_the_additive_range_fields_and_plan_evidence()
    {
        using var json = JsonDocument.Parse(ArtifactRenderReports.Create(_withEvidence, _ranged).Completeness.Bytes.AsMemory());
        var root = json.RootElement;
        root.GetProperty("schemaVersion").GetString().ShouldEqual("stage-render-completeness/v2");
        var support = root.GetProperty("support");
        support.GetProperty("version").GetString().ShouldEqual("stage-render-audited-support/v2");
        support.GetProperty("policy").GetString().ShouldEqual("AuditedOrNewerWithinDeclaredRange");
        support.GetProperty("declaredRange").GetProperty("minimumSemanticVersion").GetString().ShouldEqual(SemanticVersion.V32.ToString());
        support.GetProperty("declaredRange").GetProperty("exclusiveMajor").GetUInt32().ShouldEqual(33u);
        support.GetProperty("effective").GetProperty("policy").GetString().ShouldEqual("Audited");
        root.GetProperty("planEvidence")[0].GetProperty("code").GetString().ShouldEqual("STAGE-DEGRADE-004");
        root.GetProperty("outcome").GetString().ShouldEqual("Degraded");
    }

    [Fact]
    void should_not_change_the_score_or_the_inventory()
    {
        using var plain = JsonDocument.Parse(ArtifactRenderReports.Create(_plan, _context).Completeness.Bytes.AsMemory());
        using var ranged = JsonDocument.Parse(ArtifactRenderReports.Create(_withEvidence, _ranged).Completeness.Bytes.AsMemory());
        ranged.RootElement.GetProperty("inventory").GetRawText().ShouldEqual(plain.RootElement.GetProperty("inventory").GetRawText());
        _withEvidence.Inventory!.Constructs.SelectMany(construct => construct.Degradations).Any(gap => gap.Evidence == _evidence).ShouldBeFalse();
    }

    [Fact]
    void should_keep_the_v1_shape_without_a_declared_range()
    {
        using var json = JsonDocument.Parse(ArtifactRenderReports.Create(_withEvidence, _context).Completeness.Bytes.AsMemory());
        json.RootElement.GetProperty("schemaVersion").GetString().ShouldEqual("stage-render-completeness/v1");
        json.RootElement.TryGetProperty("planEvidence", out _).ShouldBeFalse();
        json.RootElement.GetProperty("support").TryGetProperty("declaredRange", out _).ShouldBeFalse();
    }

    [Fact]
    void should_change_the_digest_and_promote_the_evidence_when_strict()
    {
        _withEvidence.Digest.ShouldNotEqual(_plan.Digest);
        var strict = _withEvidence.WithAcceptance(_withEvidence.Degradations, strict: true);
        strict.PlanEvidence.ShouldEqual(_withEvidence.PlanEvidence);
        strict.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-DEGRADE-004").Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
        strict.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
    }

    [Fact]
    void should_refuse_an_input_outside_the_audited_pairs_and_the_range()
    {
        var other = new ArtifactRenderVersionPair(LanguageVersion.V32, SemanticVersion.V32);
        var outside = _ranged with { Support = new ArtifactRenderAuditedSupport([other]) { DeclaredRange = _ranged.Support.DeclaredRange } };
        Assert.Throws<InvalidArtifactRenderContract>(() => ArtifactRenderReports.Create(_withEvidence, outside));
    }

    [Fact]
    void should_refuse_plan_evidence_that_matches_no_original_warning()
    {
        var stray = new ArtifactRenderDegradation(new(ArtifactRenderDegradationCodes.NewerEsmWithinDeclaredRange, ArtifactRenderDiagnosticSeverity.Warning, "Other.", Id(9)), "Fix.");
        Assert.Throws<InvalidArtifactRenderContract>(() => _plan.WithPlanEvidence([stray]));
    }
}
