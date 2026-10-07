// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_a_role_only_policy.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_a_role_only_policy(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_and_release_without_warnings() => (fixture.DebugWarnings + fixture.ReleaseWarnings).ShouldBeEmpty();
    [Fact] void should_allow_and_deny_through_both_arc_pipelines() => fixture.Results.Select(result => result.Outcome).ShouldContainOnly(["Passed", "Passed", "Passed"]);

    public class context : a_portable_policy_pipeline
    {
        protected override string ModelSource => when_rendering_portable_authorization.Source
            .Replace("authenticated and (role \"Staff\" or claim \"department\" matches \"Sales\")", "role \"Staff\"", StringComparison.Ordinal)
            .Replace("        authorize Owns\n", string.Empty, StringComparison.Ordinal)
            .Replace("        authorize OwnQuery\n", string.Empty, StringComparison.Ordinal);
        protected override IEnumerable<(string Name, string Principal, bool Allowed)> Vectors =>
        [
            ("allow_an_unauthenticated_role", "Guest(new Claim(ClaimTypes.Role, \"Staff\"))", true),
            ("deny_a_missing_role_without_events_or_data", "Guest()", false),
            ("deny_a_role_with_different_casing", "Guest(new Claim(ClaimTypes.Role, \"staff\"))", false)
        ];

        Task Because() => VerifyPipeline();
    }
}
#endif
