// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_append_refusals;

public class with_an_append_refused_with_other_expectations : given.a_specification_in_a_slice
{
    void Establish()
    {
        Prepare(with_an_append_a_unique_event_constraint_refuses.Source);
        _specification = _specification with { GivenCaller = new(true, [], []) };
    }

    void Because() => Validate();

    [Fact] void should_refuse_the_specification() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
}
