// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ProjectionConventionInput;

public class when_reading_projection_conventions : Specification
{
    [Theory]
    [InlineData("classic")]
    [InlineData("model-bound")]
    public void should_round_trip_the_explicit_selection(string convention)
    {
        var input = ProjectionConventionInput.Read("Projects", $$"""{"version":1,"application":"Projects","projectionConvention":"{{convention}}"}""");
        ProjectionConventionInput.TryRead(CratisRendering.WithProjectionConvention(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), input), "Projects", out var selected).ShouldBeTrue();
        selected.ShouldEqual(convention == "model-bound");
        input.Name.ShouldEqual(ProjectionConventionInput.Name);
        input.Version.ShouldEqual("1");
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("{}")]
    [InlineData("{\"version\":1.5,\"application\":\"Projects\",\"projectionConvention\":\"classic\"}")]
    [InlineData("{\"version\":2,\"application\":\"Projects\",\"projectionConvention\":\"classic\"}")]
    [InlineData("{\"version\":1,\"application\":\"Other\",\"projectionConvention\":\"classic\"}")]
    [InlineData("{\"version\":1,\"application\":\"Projects\",\"projectionConvention\":null}")]
    [InlineData("{\"version\":1,\"application\":\"Projects\",\"projectionConvention\":\"other\"}")]
    [InlineData("{\"version\":1,\"application\":\"Projects\",\"projectionConvention\":\"classic\",\"extra\":true}")]
    [InlineData("{\"version\":1,\"application\":\"Projects\",\"projectionConvention\":\"classic\",\"projectionConvention\":\"model-bound\"}")]
    public void should_reject_invalid_or_stale_configuration(string json) =>
        Assert.Throws<InvalidArtifactRenderContract>(() => ProjectionConventionInput.Read("Projects", json));

    [Fact]
    public void should_leave_absent_profiles_classic()
    {
        ProjectionConventionInput.TryRead(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), "Projects", out var selected).ShouldBeTrue();
        selected.ShouldBeFalse();
    }

    [Theory]
    [InlineData("2")]
    [InlineData("0")]
    public void should_reject_an_unsupported_input_version(string version)
    {
        var original = ProjectionConventionInput.Create("Projects", "model-bound");
        var invalid = ArtifactRenderInput.Create(original.Name, version, original.Bytes);
        var profile = CratisRendering.WithProjectionConvention(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), invalid);
        var plan = CratisRendering.PlanScaffold(new("Projects", "Projects", "Projects") { Profile = profile });
        plan.Success.ShouldBeFalse();
        Assert.Empty(plan.Artifacts);
    }

    [Fact]
    public void should_reject_malformed_inputs_at_profile_admission()
    {
        var invalid = ArtifactRenderInput.Create(ProjectionConventionInput.Name, "1", [.. Encoding.UTF8.GetBytes("{}")]);
        var profile = CratisRendering.WithProjectionConvention(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), invalid);
        var plan = CratisRendering.PlanScaffold(new("Projects", "Projects", "Projects") { Profile = profile });
        plan.Success.ShouldBeFalse();
        Assert.Empty(plan.Artifacts);
    }

    [Fact]
    public void should_not_accept_an_input_with_another_identity() =>
        Assert.Throws<InvalidArtifactRenderContract>(() => CratisRendering.WithProjectionConvention(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), ReducerConventionInput.Create("Projects", "self")));
}
