// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_invoked_constraint_event_expectations : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish() => Prepare(invocation_constraint_model.Source
        .Replace("            on refused by constraint OneCompletion\n              acknowledge\n", string.Empty, StringComparison.Ordinal)
        .Replace("        then invokes Complete", "        then Completed", StringComparison.Ordinal));
    void Because() => Validate();
    [Fact] void should_refuse_unenforced_event_outcomes() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
}
