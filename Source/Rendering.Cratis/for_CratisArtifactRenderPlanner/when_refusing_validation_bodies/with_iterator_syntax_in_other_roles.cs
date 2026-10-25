// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_validation_bodies;

public class with_iterator_syntax_in_other_roles : Specification
{
    [Theory]
    [InlineData("yield return \"message\";")]
    [InlineData("return $\"{context.Value}\" == \"1\";")]
    void should_not_widen_boolean_rule_admission(string body)
    {
        var plan = opaque_policy_model.Plan(validation_body_model.Load(validation_body_model.Command(body)));
        rendering_refusals.Rejected(plan, "STAGE-ESM-022");
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022");
    }

    [Theory]
    [InlineData("reducer", "yield return \"message\";")]
    [InlineData("reducer", "return $\"{context.Event.Amount}\";")]
    [InlineData("policy", "yield return \"message\";")]
    [InlineData("policy", "return $\"{context.Occurred}\";")]
    void should_keep_shared_pure_parsing_closed_for_reducers_and_policies(string role, string body) =>
        PureTransitionAdmission.ParseBody(body, new HashSet<string>(), role).Rejection!.Code.ShouldEqual("STAGE-ESM-022");
}
#endif
