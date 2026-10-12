// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions;

public class with_a_matching_flat_key : given.a_projected_reaction
{
    void Establish() => CompileProjected();
    Task Because() => Execute();
    [Fact] void should_project_the_produced_fact_like_the_reference() => AssertMatchesReference();
    [Fact] void should_materialize_the_expected_instance() => _result.Trace!.ReadModels.Count.ShouldEqual(1);
}
