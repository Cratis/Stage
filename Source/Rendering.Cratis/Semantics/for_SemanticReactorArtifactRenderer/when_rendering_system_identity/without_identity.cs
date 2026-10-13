// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_system_identity;

public class without_identity : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    string _source = string.Empty;
    void Because() => _source = SemanticReactorArtifactRenderer.Render(_located, _located.Slice.Reactions.Single(reaction => reaction.Name == "CompletePositive"), _context).Content;
    [Fact] void should_leave_the_invocation_without_a_principal() => _source.ShouldNotContain("ExecuteCommandsAsSystemAttribute");
}
