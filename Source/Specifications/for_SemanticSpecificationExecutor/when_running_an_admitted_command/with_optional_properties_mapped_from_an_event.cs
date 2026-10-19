// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_optional_properties_mapped_from_an_event : given.a_projection_with_optional_properties
{
    SemanticSpecificationRunRecord _result = null!;
    protected override bool MapValues => true;

    void Establish() => Configure();

    async Task Because() => _result = Assert.Single((await new SemanticSpecificationExecutor().Run(_plan, new([_specification.Id]), new())).Results);

    [Fact] void should_compare_projected_optional_values() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_agree_with_the_reference() => new SemanticSpecificationRunner().Run(_plan, _specification.Id).Passed.ShouldBeTrue();
}
