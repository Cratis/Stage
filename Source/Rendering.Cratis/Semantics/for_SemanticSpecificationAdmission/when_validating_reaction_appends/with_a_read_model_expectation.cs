// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_a_read_model_expectation : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish() => _specification = _specification with { ThenReadModels = [new(_specification.Id, SemanticValue.Text("one"), [])] };
    void Because() => Validate();
    [Fact] void should_refuse_unmaterialized_read_models() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
}
