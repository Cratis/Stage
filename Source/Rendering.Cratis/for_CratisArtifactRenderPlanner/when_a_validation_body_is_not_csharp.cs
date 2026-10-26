// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateViewArtifactRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_a_validation_body_is_not_csharp
{
    [Theory]
    [InlineData("missing")]
    [InlineData("descriptor")]
    [InlineData("member")]
    [InlineData("capability")]
    public void should_not_turn_invalid_envelopes_into_language_gaps(string change)
    {
        var loaded = validation_body_model.Load(validation_body_model.Command("return context.Value > 0;"));
        loaded = change switch
        {
            "missing" => loaded with { ImplementationContents = loaded.ImplementationContents.Clear() },
            "descriptor" => loaded with { TypedContextDescriptors = [] },
            "member" => loaded with { TypedContextDescriptors = [loaded.TypedContextDescriptors.Single() with { Members = loaded.TypedContextDescriptors.Single().Members.RemoveAt(0) }] },
            _ => loaded with { ImplementationRequirements = [loaded.ImplementationRequirements.Single() with { RequiredCapability = "provider-defined" }] }
        };
        var original = opaque_policy_model.Plan(loaded);
        var unsupported = opaque_policy_model.Plan(loaded with { ImplementationRequirements = [loaded.ImplementationRequirements.Single() with { Language = "java" }] });
        Assert.False(unsupported.Success);
        Assert.Empty(unsupported.Artifacts);
        Assert.Empty(unsupported.ImplementationGaps);
        Assert.DoesNotContain(unsupported.Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-007");
        Assert.Equal(original.Diagnostics.Select(diagnostic => diagnostic.Code), unsupported.Diagnostics.Select(diagnostic => diagnostic.Code));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_still_refuse_a_command_validation_and_name_the_language(bool file)
    {
        const string source = "return context.Value > 0;";
        var loaded = file ? await when_performer_languages_are_not_supported.LoadFile(validation_body_model.Command(source), "java") :
            await when_rendering_a_pure_reducer.Load(validation_body_model.Command(source).Replace("```csharp", "```java", StringComparison.Ordinal));
        AssertRefused(opaque_policy_model.Plan(loaded));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_still_refuse_a_concept_validation_and_name_the_language(bool file)
    {
        var loaded = validation_body_model.Load(validation_body_model.IteratorConcept("yield return \"invalid\";"));
        var requirement = loaded.ImplementationRequirements.Single() with { Language = file ? null : "java", File = file ? "Body.java" : null };
        Assert.Equal(SemanticImplementationRole.ConceptValidation, requirement.Role);
        AssertRefused(opaque_policy_model.Plan(loaded with { ImplementationRequirements = [requirement] }));
    }

    static void AssertRefused(ArtifactRenderPlan plan)
    {
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        var diagnostic = Assert.Single(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019");
        Assert.Contains("language 'java'", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-DEGRADE-003" || diagnostic.Code == "STAGE-IMPL-007");
    }
}
#endif
