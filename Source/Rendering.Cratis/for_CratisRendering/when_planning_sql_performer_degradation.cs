// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_sql_performer_degradation : given.a_source_plan
{
    CratisPlanResult _normal = null!;

    void Establish() => File.WriteAllText(Path.Combine(_root, "performer.play"),
        when_strict_planning_retains_performer_metadata.PerformerSource
            .Replace("```csharp", "```sql", StringComparison.Ordinal)
            .Replace("return missing;", "select * from people", StringComparison.Ordinal));

    async Task Because()
    {
        _normal = await From("performer.play");
        _planOptions = _planOptions with { Strict = true };
        _result = await From("performer.play");
    }

    [Fact]
    void should_report_sql_as_degraded_with_warning_remedy_and_inventory_in_default_mode()
    {
        var plan = _normal.Plan!;
        Assert.True(plan.Success);
        Assert.False(plan.Strict);
        Assert.Equal(ArtifactRenderOutcome.Degraded, plan.Outcome);
        Assert.Empty(plan.ImplementationGaps);
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-006");
        var degradation = Assert.Single(plan.Degradations);
        Assert.Equal(ArtifactRenderDegradationCodes.InlineMismatch, degradation.Diagnostic.Code);
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Warning, degradation.Diagnostic.Severity);
        Assert.Contains("language 'sql'", degradation.Diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("Query 'FindPeople'", degradation.Diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(ImplementationLanguage.Remedy, degradation.Remedy);
        Assert.Contains(degradation.Diagnostic, plan.Diagnostics);
        Assert.NotNull(plan.Inventory);
        var query = Assert.Single(plan.Inventory.Constructs, construct => construct.Identity == degradation.Diagnostic.Artifact);
        Assert.Equal(ArtifactRenderOutcome.Degraded, query.Outcome);
        var gap = Assert.Single(query.Degradations);
        Assert.Equal(degradation, gap.Evidence);
        Assert.Equal(ArtifactRenderPlaceholderKind.ThrowingOperationBody, gap.PlaceholderKind);
    }

    [Fact]
    void should_promote_only_strict_diagnostics_and_restore_original_sql_evidence()
    {
        var strict = _result.Plan!;
        Assert.False(strict.Success);
        Assert.True(strict.Strict);
        Assert.True(strict.CanPublishArtifacts);
        Assert.Empty(strict.ImplementationGaps);
        Assert.Equal(ArtifactRenderOutcome.Degraded, strict.Outcome);
        var evidence = Assert.Single(strict.Degradations);
        Assert.Equal(Assert.Single(_normal.Plan!.Degradations), evidence);
        Assert.Contains(strict.Diagnostics, diagnostic => diagnostic.Code == ArtifactRenderDegradationCodes.InlineMismatch && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        var restored = strict.WithAcceptance(strict.Degradations, strict: false);
        Assert.True(restored.Success);
        Assert.Contains(evidence.Diagnostic, restored.Diagnostics);
        Assert.Equal(ArtifactRenderOutcome.Degraded, restored.Outcome);
    }
}
