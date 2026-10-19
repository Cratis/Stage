// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_an_admitted_command;

public class with_only_optional_properties_and_a_mapping_free_from_block : given.a_projection_with_optional_properties
{
    SemanticSpecificationRunRecord _result = null!;

    protected override bool OnlyOptional => true;

    void Establish() => Configure();

    async Task Because() => _result = Assert.Single((await new SemanticSpecificationExecutor().Run(_plan, new([_specification.Id]), new())).Results);

    [Fact] void should_be_unsupported() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_explain_that_no_instance_materializes() => _result.Unsupported!.Details.ShouldContain("does not materialize a Chronicle read model");
    [Fact] void should_still_pass_in_the_reference() => new SemanticSpecificationRunner().Run(_plan, _specification.Id).Passed.ShouldBeTrue();
}
