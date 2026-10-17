// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_validation_bodies;

public class with_an_unsafe_iterator : Specification
{
    [Theory]
    [InlineData("yield return System.IO.File.ReadAllText(\"secret\");")]
    [InlineData("yield return $\"{System.DateTimeOffset.Now}\";")]
    [InlineData("while (true) yield return \"message\";")]
    [InlineData("yield return $\"{context.Artifact}\";")]
    [InlineData("yield return $\"{context.Artifact.Amount.ToString()}\";")]
    [InlineData("yield return $\"{0.1d}\";")]
    [InlineData("yield return \"message\"; yield break;")]
    void should_keep_the_closed_pure_gate(string body)
    {
        var plan = opaque_policy_model.Plan(validation_body_model.Load(validation_body_model.IteratorCommand(body)));
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022");
    }

    [Theory]
    [InlineData("Tenant")]
    [InlineData("CausedBy")]
    [InlineData("Occurred")]
    void should_refuse_unsupplied_metadata_by_member_name(string member)
    {
        var plan = opaque_policy_model.Plan(validation_body_model.Load(validation_body_model.IteratorCommand($"yield return $\"{{context.{member}}}\";")));
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-015" && diagnostic.Message.Contains(member, StringComparison.Ordinal));
    }
}
#endif
