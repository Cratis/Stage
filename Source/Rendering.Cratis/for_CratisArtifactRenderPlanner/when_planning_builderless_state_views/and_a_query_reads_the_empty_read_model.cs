// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_builderless_state_views;

public class and_a_query_reads_the_empty_read_model : Specification
{
    string _builderless = string.Empty;
    string _built = string.Empty;

    void Because()
    {
        _builderless = Source(builderless_state_view_model.Plan(builderless_state_view_model.Queried));
        _built = Source(builderless_state_view_model.Plan(builderless_state_view_model.BuiltQuery));
    }

    [Fact] void should_keep_the_query() => _builderless.ShouldContain("ById");
    [Fact] void should_not_change_anonymous_access_compared_with_a_built_query() => _builderless.Contains("AllowAnonymous", StringComparison.Ordinal).ShouldEqual(_built.Contains("AllowAnonymous", StringComparison.Ordinal));
    [Fact] void should_keep_the_authorization_attributes_of_a_built_query() => Authorization(_builderless).ShouldEqual(Authorization(_built));

    static string Source(ArtifactRenderPlan plan)
    {
        plan.Success.ShouldBeTrue();
        return string.Join('\n', plan.Artifacts.Select(_ => System.Text.Encoding.UTF8.GetString(_.Bytes.AsSpan())));
    }

    static string[] Authorization(string source) => [.. source.Split('\n').Where(_ => _.Contains("Authorize", StringComparison.Ordinal) || _.Contains("AllowAnonymous", StringComparison.Ordinal)).Select(_ => _.Trim()).Distinct()];
}
