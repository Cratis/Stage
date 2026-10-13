// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_system_identity;

public class without_roles : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    string _source = string.Empty;
    void Establish() => SetContext(Compile(Source.Replace("      reaction Acknowledge", "      reaction Acknowledge\n        runs as system", StringComparison.Ordinal)));
    void Because() => _source = SemanticReactorArtifactRenderer.Render(_located, _located.Slice.Reactions.Single(reaction => reaction.Name == "Acknowledge"), _context).Content;
    [Fact] void should_render_the_identity_even_without_invocations() => _source.ShouldContain("[global::Cratis.Arc.Chronicle.Reactors.ExecuteCommandsAsSystemAttribute]");
}
