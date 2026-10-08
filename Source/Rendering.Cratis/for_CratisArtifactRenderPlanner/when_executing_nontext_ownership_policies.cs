// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_nontext_ownership_policies.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_nontext_ownership_policies(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_and_release_without_warnings() => (fixture.DebugWarnings + fixture.ReleaseWarnings).ShouldBeEmpty();
    [Fact] void should_deny_decimal_claims_and_allow_the_alternative_role_through_the_arc_pipeline() => fixture.Results.Select(result => result.Outcome).ShouldContainOnly(["Passed", "Passed", "Passed", "Passed"]);

    public class context : a_portable_policy_pipeline
    {
        protected override string ModelSource => when_admitting_nontext_claim_targets.Source;
        protected override bool IncludeQuery => false;
        protected override string IdentifierExpression => "new InvoiceId(decimal.Parse(key, System.Globalization.CultureInfo.InvariantCulture))";
        protected override string Key => "123.45";
        protected override IEnumerable<(string Name, string Principal, bool Allowed)> Vectors =>
        [
            ("deny_a_claim_equal_to_the_decimal_text", "Authenticated(new Claim(\"owner\", \"123.45\"))", false),
            ("deny_a_missing_claim", "Authenticated()", false),
            ("allow_the_alternative_role", "Authenticated(new Claim(ClaimTypes.Role, \"Staff\"), new Claim(\"owner\", \"123.45\"))", true),
            ("deny_a_guest", "Guest()", false)
        ];

        Task Because() => VerifyPipeline();
    }
}
#endif
