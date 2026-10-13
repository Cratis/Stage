// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationClock;

public class when_admitting_an_authored_clock : Specification
{
    SemanticSpecification _specification = null!;

    void Establish() => _specification = invoice_model.Compile(a_clock_model.Source).Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();

    [Fact] void should_admit_a_v6_command_clock() => SemanticVersionFeatures.InSpecification(_specification).ShouldBeEmpty();
    [Fact] void should_render_the_authored_utc_instant_first()
    {
        var plan = invoice_model.Plan(invoice_model.Compile(a_clock_model.Source));
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics.Select(value => value.Message)));
        var code = System.Text.Encoding.UTF8.GetString(plan.Artifacts.Single(value => value.RelativePath.EndsWith("when_at_the_authored_instant.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        code.ShouldContain("void Establish()\n    {\n        _scenario.Clock(new global::System.DateTimeOffset(639397512001230000L, global::System.TimeSpan.Zero));");
    }
    [Theory]
    [InlineData("2027-03-04T10:00:00.1230001Z")]
    [InlineData("2027-03-04T10:00:00.123Z")]
    [InlineData("1969-03-04T10:00:00.1230000Z")]
    public void should_refuse_an_inexact_or_noncanonical_clock(string clock) => SemanticVersionFeatures.InSpecification(_specification with { GivenClock = clock }).Single().Capability.ShouldEqual(StageExecutionCapability.Time);
    [Fact] void should_refuse_advancing_the_clock() => SemanticVersionFeatures.InSpecification(_specification with { When = null, WhenClock = "2027-03-04T11:00:00.1230000Z" }).Single().Capability.ShouldEqual(StageExecutionCapability.Time);
    [Fact] void should_refuse_query_only_clocks() => SemanticVersionFeatures.InSpecification(_specification with { When = null }).Single().Capability.ShouldEqual(StageExecutionCapability.Time);
    [Fact] void should_refuse_append_clocks() => SemanticVersionFeatures.InSpecification(_specification with { When = null, WhenAppended = new(_specification.ThenEvents[0].EventContract, _specification.ThenEvents[0].Values) }).Single().Capability.ShouldEqual(StageExecutionCapability.Time);
}
