// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_denied_operations;

public class with_a_denied_cascade_invocation : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish()
    {
        Prepare(for_SemanticSpecificationArtifactRenderer.when_rendering_reaction_cascades.given.an_inventory.OneSlice);
        _context.DeniedOperations.Add(_located.Slice.Commands.Single(command => command.Name == "RecordStockLevel").Id, "Unsupported validation rule.");
    }

    void Because() => Validate();

    [Fact] void should_refuse_the_cascade_once() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-011");
    [Fact] void should_identify_the_original_specification() => _diagnostics.Single().Artifact.ShouldEqual(_specification.Id);
    [Fact] void should_name_the_denied_invoked_command() => _diagnostics.Single().Message.ShouldContain("command 'RecordStockLevel' denies every invocation (STAGE-DEGRADE-002)");
}
