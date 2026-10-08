// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using bool_context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_nontext_ownership_policies.bool_context;
using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_nontext_ownership_policies.context;
using int_context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_nontext_ownership_policies.int_context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_nontext_ownership_policies(context fixture, int_context wholeNumbers, bool_context truthValues) : IClassFixture<context>, IClassFixture<int_context>, IClassFixture<bool_context>
{
    [Theory]
    [InlineData("Decimal")]
    [InlineData("Int")]
    [InlineData("Bool")]
    public void should_build_debug_and_release_without_warnings(string primitive) => (For(primitive).DebugWarnings + For(primitive).ReleaseWarnings).ShouldBeEmpty();

    [Theory]
    [InlineData("Decimal")]
    [InlineData("Int")]
    [InlineData("Bool")]
    public void should_deny_nontext_and_empty_query_claims_and_allow_the_alternative_role_through_the_arc_pipeline(string primitive) =>
        For(primitive).Results.Select(result => result.Outcome).ShouldContainOnly(["Passed", "Passed", "Passed", "Passed"]);

    context For(string primitive) => primitive switch
    {
        "Int" => wholeNumbers,
        "Bool" => truthValues,
        _ => fixture
    };

    public class context : a_portable_policy_pipeline
    {
        protected virtual string Primitive => "Decimal";
        protected override string ModelSource => when_admitting_nontext_claim_targets.QuerySource.Replace("concept InvoiceId : Decimal", $"concept InvoiceId : {Primitive}", StringComparison.Ordinal);
        protected override bool VerifyEmptyQueryDenial => true;
        protected override string IdentifierExpression => Primitive switch
        {
            "Int" => "new InvoiceId(int.Parse(key, System.Globalization.CultureInfo.InvariantCulture))",
            "Bool" => "new InvoiceId(bool.Parse(key))",
            _ => "new InvoiceId(decimal.Parse(key, System.Globalization.CultureInfo.InvariantCulture))"
        };
        protected override string Key => Primitive switch
        {
            "Int" => "123",
            "Bool" => "True",
            _ => "123.45"
        };
        protected override IEnumerable<(string Name, string Principal, bool Allowed)> Vectors =>
        [
            ("deny_a_claim_equal_to_the_scalar_text", $"Authenticated(new Claim(\"owner\", \"{Key}\"))", false),
            ("deny_a_missing_claim", "Authenticated()", false),
            ("allow_the_alternative_role", $"Authenticated(new Claim(ClaimTypes.Role, \"Staff\"), new Claim(\"owner\", \"{Key}\"))", true),
            ("deny_a_guest", "Guest()", false)
        ];

        Task Because() => VerifyPipeline();
    }

    public class int_context : context
    {
        protected override string Primitive => "Int";
    }

    public class bool_context : context
    {
        protected override string Primitive => "Bool";
    }
}
#endif
