// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_a_claim_only_policy.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_a_claim_only_policy(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_and_release_without_warnings() => (fixture.DebugWarnings + fixture.ReleaseWarnings).ShouldBeEmpty();
    [Fact] void should_allow_and_deny_through_both_arc_pipelines() => fixture.Results.Select(result => result.Outcome).ShouldContainOnly(["Passed", "Passed", "Passed", "Passed", "Passed", "Passed", "Passed", "Passed", "Passed"]);
    [Fact] void should_assert_guest_command_denial() => fixture.Results.Any(result => result.Name.EndsWith("should_be_denied", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_assert_guest_query_denial() => fixture.Results.Any(result => result.Name.EndsWith("should_be_unauthorized", StringComparison.Ordinal)).ShouldBeTrue();

    public class context : a_portable_policy_pipeline
    {
        protected override string ModelSource => WithGuestDenials(when_rendering_portable_authorization.Source
            .Replace("authenticated and (role \"Staff\" or claim \"department\" matches \"Sales\")", "claim \"department\" matches \"Sales\"", StringComparison.Ordinal)
            .Replace("        authorize Owns\n", string.Empty, StringComparison.Ordinal)
            .Replace("        authorize OwnQuery\n", string.Empty, StringComparison.Ordinal));
        protected override IEnumerable<(string Name, string Principal, bool Allowed)> Vectors =>
        [
            ("allow_a_matching_repeated_claim", "Authenticated(new Claim(\"department\", \"Other\"), new Claim(\"DEPARTMENT\", \"Sales\"))", true),
            ("deny_a_missing_claim_without_events_or_data", "Authenticated()", false),
            ("deny_a_claim_value_with_different_casing", "Authenticated(new Claim(\"department\", \"sales\"))", false),
            ("deny_a_guest_without_events_or_data", "Guest()", false)
        ];

        Task Because() => VerifyPipeline();
    }
}
#endif
