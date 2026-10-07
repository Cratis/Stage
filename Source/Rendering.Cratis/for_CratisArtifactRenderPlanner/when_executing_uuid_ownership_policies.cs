// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_uuid_ownership_policies.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_uuid_ownership_policies(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_and_release_without_warnings() => (fixture.DebugWarnings + fixture.ReleaseWarnings).ShouldBeEmpty();
    [Fact] void should_compare_typed_uuid_values_and_deny_invalid_or_missing_claims() => fixture.Results.Select(result => result.Outcome).ShouldContainOnly(["Passed", "Passed", "Passed", "Passed", "Passed"]);

    public class context : a_portable_policy_pipeline
    {
        protected override string ModelSource => when_rendering_portable_authorization.Source
            .Replace("concept InvoiceId : String", "concept InvoiceId : Uuid", StringComparison.Ordinal)
            .Replace("  authorize Access\n", string.Empty, StringComparison.Ordinal)
            .Replace("claim \"owner\" matches subject and", "claim \"owner\" matches subject and claim \"owner\" matches invoiceId and", StringComparison.Ordinal);
        protected override string IdentifierExpression => "new InvoiceId(new Guid(key))";
        protected override string Key => "3fa85f64-5717-4562-b3fc-2c963f66afa6";
        protected override IEnumerable<(string Name, string Principal, bool Allowed)> Vectors =>
        [
            ("allow_a_typed_uuid_match", "Guest(new Claim(\"OWNER\", \"{3FA85F64-5717-4562-B3FC-2C963F66AFA6}\"), new Claim(\"region\", \"North\"))", true),
            ("deny_a_different_uuid", "Guest(new Claim(\"owner\", \"4fa85f64-5717-4562-b3fc-2c963f66afa7\"), new Claim(\"region\", \"North\"))", false),
            ("deny_a_malformed_claim", "Guest(new Claim(\"owner\", \"not-a-uuid\"), new Claim(\"region\", \"North\"))", false),
            ("deny_a_missing_claim", "Guest(new Claim(\"region\", \"North\"))", false),
            ("allow_a_repeated_claim_with_one_parseable_match", "Guest(new Claim(\"owner\", \"not-a-uuid\"), new Claim(\"owner\", \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"), new Claim(\"region\", \"North\"))", true)
        ];

        Task Because() => VerifyPipeline();
    }
}
#endif
