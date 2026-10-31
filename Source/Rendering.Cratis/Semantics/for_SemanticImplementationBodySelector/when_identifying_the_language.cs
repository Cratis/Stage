// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticImplementationBodySelector;

public class when_identifying_the_language
{
    [Theory]
    [InlineData("csharp", null, "Selected", "csharp")]
    [InlineData(null, "Body.cs", "Selected", "csharp")]
    [InlineData("csharp", "Body.cs", "Selected", "csharp")]
    [InlineData("java", null, "NotInTargetLanguage", "java")]
    [InlineData("csharp", "Body.java", "NotInTargetLanguage", "csharp")]
    [InlineData("cs", null, "NotInTargetLanguage", "cs")]
    [InlineData(null, "Body.java", "UndeterminedLanguage", null)]
    [InlineData(null, null, "UndeterminedLanguage", null)]
    [InlineData(null, "Body.CS", "UndeterminedLanguage", null)]
    public void should_select_only_the_canonical_target_language(string? language, string? file, string outcome, string? effectiveLanguage)
    {
        var requirement = with_attached_bodies_beside_productions.Request().ImplementationRequirements.Single() with { Language = language, File = file };
        var selection = SemanticImplementationBodySelector.Select(requirement);

        Assert.Equal(outcome, selection.Outcome.ToString());
        Assert.Same(requirement, selection.Requirement);
        Assert.Equal(requirement.RequirementId, selection.ContentKey);
        Assert.Equal(effectiveLanguage, SemanticImplementationBodySelector.EffectiveLanguage(requirement));
        Assert.Equal(effectiveLanguage is null ? [] : new[] { effectiveLanguage }, selection.AuthoredLanguages);
        Assert.Equal(outcome == "Selected", SemanticImplementationBodySelector.IsTarget(requirement));
        if (outcome == "Selected") Assert.Same(requirement, selection.Selected);
        else Assert.Null(selection.Selected);
    }
}
