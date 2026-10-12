// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_admitting_automation_read_surfaces : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    const string ReadSurface = """
            slice StateView Lookup
              readmodel Summary
                id WorkId
                amount Decimal
              query ById => Summary?
                by id WorkId
              projection SummaryProjection => Summary
                from Started key id
                  id = id
                  amount = amount
        """;

    void Establish() => SetContext(Compile("concept WorkId : String\n" + Source.Replace("id String", "id WorkId", StringComparison.Ordinal) + "\n" + ReadSurface));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_refuse_foreign_read_artifacts_like_a_state_view(bool queryOnly)
    {
        var lookup = _context.SelectedSlices().Single(slice => slice.Slice.Kind == SemanticSliceKind.StateView).Slice;
        var slice = _located.Slice with
        {
            Queries = queryOnly ? lookup.Queries : [],
            Projections = queryOnly ? [] : lookup.Projections
        };
        var automation = SemanticCratisAdmission.Evaluate(_context, [_located with { Slice = slice }]);
        var stateView = SemanticCratisAdmission.Evaluate(_context, [_located with { Slice = slice with { Kind = SemanticSliceKind.StateView, Commands = [], Reactions = [] } }]);
        automation.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly(["STAGE-ESM-007"]);
        stateView.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly(["STAGE-ESM-007"]);
        automation.Any(diagnostic => diagnostic.Artifact == slice.Id).ShouldBeTrue();
    }

    [Fact]
    public void should_refuse_a_compiled_query_only_surface()
    {
        const string query = """
                  query ForeignLookup => Summary?
                    by id WorkId
            """;
        SetContext(Compile("concept WorkId : String\n" + Source.Replace("id String", "id WorkId", StringComparison.Ordinal) + "\n" + query + "\n" + ReadSurface));
        _located.Slice.ReadModels.ShouldBeEmpty();
        _located.Slice.Projections.ShouldBeEmpty();
        Plan();
        _plan.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly(["STAGE-ESM-007"]);
        _plan.Artifacts.ShouldBeEmpty();
    }
}
