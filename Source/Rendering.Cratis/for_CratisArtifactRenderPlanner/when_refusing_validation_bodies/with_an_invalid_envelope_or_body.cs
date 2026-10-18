// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_validation_bodies;

public class with_an_invalid_envelope_or_body
{
    [Theory]
    [InlineData("missing", "STAGE-ESM-020")]
    [InlineData("descriptor", "STAGE-ESM-021")]
    [InlineData("member", "STAGE-ESM-021")]
    [InlineData("language", "STAGE-ESM-019")]
    [InlineData("compile", "STAGE-ESM-019")]
    [InlineData("pure", "STAGE-ESM-022")]
    public void should_refuse_without_artifacts(string change, string code)
    {
        var body = change switch { "compile" => "return context.Missing;", "pure" => "return System.DateTime.Now.Year > 0;", _ => "return context.Value > 0;" };
        var loaded = validation_body_model.Load(validation_body_model.Command(body));
        loaded = change switch
        {
            "missing" => loaded with { ImplementationContents = loaded.ImplementationContents.Clear() },
            "descriptor" => loaded with { TypedContextDescriptors = [] },
            "member" => loaded with { TypedContextDescriptors = [loaded.TypedContextDescriptors.Single() with { Members = loaded.TypedContextDescriptors.Single().Members.RemoveAt(0) }] },
            "language" => loaded with { ImplementationRequirements = [loaded.ImplementationRequirements.Single() with { Language = "javascript" }] },
            _ => loaded
        };
        var plan = opaque_policy_model.Plan(loaded);
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == code);
    }
}
#endif
