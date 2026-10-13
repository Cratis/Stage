// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_an_absence_expectation : for_SemanticReactionSpecificationRenderer.given.a_reaction_specification
{
    void Establish() => _specification = _specification with { ThenAbsentReadModels = [new(_specification.Id, SemanticValue.Text("one"))] };
    void Because() => Validate();
    [Fact] void should_refuse_absence_outside_the_reactor_scenario() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
}
