// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticImplementationBodySelector;

public class when_describing_a_language_refusal
{
    [Fact]
    public void should_name_the_member_requirement_and_target_language()
    {
        var request = with_attached_bodies_beside_productions.Request(language: "java");
        var requirement = Assert.Single(request.ImplementationRequirements);
        var owner = requirement.Source.SemanticId;
        var selection = SemanticImplementationBodySelector.Select(requirement);
        var diagnostic = SemanticImplementationBodySelector.LanguageRefusal(selection, "Command handler for 'ApplyLateFee'", owner);

        Assert.Equal("STAGE-IMPL-LANGUAGE", diagnostic.Code);
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(owner, diagnostic.Artifact);
        Assert.Equal($"Command handler for 'ApplyLateFee' (requirement {requirement.RequirementId}) has implementation bodies in java but none in csharp. Add a csharp body to render it with the Cratis target.", diagnostic.Message);
    }
}
