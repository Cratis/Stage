// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_refusing_sql_only_performers;

public class with_default_and_strict_planning : given.a_source_plan
{
    readonly List<CratisPlanResult> _plans = [];

    void Establish() => File.WriteAllText(Path.Combine(_root, "performer.play"), """
        module Projects
          feature Registration
            slice StateView Listings
              readmodel PersonSummary
                personId Uuid
                name String
              query FindPeople => PersonSummary[]
                filter name String
                performer
                  ```sql
                  select * from people
                  ```
        """);

    async Task Because()
    {
        _plans.Add(await From("performer.play"));
        _planOptions = _planOptions with { Strict = true };
        _plans.Add(await From("performer.play"));
    }

    [Fact]
    void should_refuse_in_default_and_strict_modes() => Assert.All(_plans, result =>
    {
        Assert.False(result.Success);
        var plan = Assert.IsType<ArtifactRenderPlan>(result.Plan);
        Assert.Equal(ArtifactRenderOutcome.Refused, plan.Outcome);
        Assert.False(plan.CanPublishArtifacts);
    });

    [Fact]
    void should_report_a_language_error_instead_of_a_performer_gap() => Assert.All(_plans, result =>
    {
        var diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-LANGUAGE");
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("sql", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => string.Equals(diagnostic.Code, "STAGE-IMPL-006", StringComparison.Ordinal) || string.Equals(diagnostic.Code, "STAGE-ESM-019", StringComparison.Ordinal));
    });

    [Fact]
    void should_not_emit_a_stub() => Assert.All(_plans, result =>
    {
        Assert.Empty(result.Artifacts);
        Assert.Empty(result.ImplementationGaps);
        Assert.Empty(result.Plan!.Artifacts);
        Assert.Empty(result.Plan.ImplementationGaps);
    });
}
