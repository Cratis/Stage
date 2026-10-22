// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_rejecting_a_from_block;

public class with_missing_required_mappings : given.a_scoped_from_block
{
    void Establish()
    {
        _firstFrom = _firstFrom with { Mappings = [] };
        Map(_mapping);
    }

    void Because() => Evaluate();

    [Fact] void should_explain_that_no_block_establishes_all_required_properties() => ShouldExplain("At least one from block must establish every required read-model property");
    [Fact] void should_name_the_missing_properties_of_the_first_block() => ShouldExplain("From block #1 for event 'ProjectRegistered' is missing required mappings for 'name', 'visits'");
    [Fact] void should_name_the_missing_properties_of_the_second_block() => ShouldExplain("From block #2 for event 'ProjectRenamed' is missing required mappings for 'visits'");
}
