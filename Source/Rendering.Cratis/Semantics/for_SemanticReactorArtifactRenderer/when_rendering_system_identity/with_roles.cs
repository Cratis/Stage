// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_system_identity;

public class with_roles : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    string _source = string.Empty;
    void Establish() => SetContext(Compile(Source.Replace("      reaction CompletePositive", "      reaction CompletePositive\n        runs as system role \"Worker\" and role \"Automation\"", StringComparison.Ordinal)));
    void Because() => _source = SemanticReactorArtifactRenderer.Render(_located, _located.Slice.Reactions.Single(reaction => reaction.Name == "CompletePositive"), _context).Content;
    [Fact] void should_preserve_the_canonical_role_order() => _source.ShouldContain("[ExecuteCommandsAsSystem(\"Automation\", \"Worker\")]");
}
