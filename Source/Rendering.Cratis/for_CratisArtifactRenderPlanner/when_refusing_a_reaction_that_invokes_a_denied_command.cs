// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_a_reaction_that_invokes_a_denied_command : Semantics.for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish()
    {
        Prepare(InvocationSource);
        ReplaceAutomation(_located.Slice with { Specifications = [] });
        _context.DeniedOperations.Add(_located.Slice.Commands.Single().Id, "Unsupported validation rule.");
    }

    void Because() => Validate();

    [Fact] void should_refuse_the_unindexed_specification() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-011");
    [Fact] void should_identify_the_specification() => _diagnostics.Single().Artifact.ShouldEqual(_specification.Id);
    [Fact] void should_name_the_denied_command() => _diagnostics.Single().Message.ShouldContain("command 'Complete' denies every invocation (STAGE-DEGRADE-002)");
}
