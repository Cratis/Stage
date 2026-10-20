// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_append_refusals;

public class with_an_append_no_given_event_makes_violate : given.a_specification_in_a_slice
{
    void Establish() => Prepare(with_an_append_a_unique_event_constraint_refuses.Source
        .Replace("        given Dispatched\n          for \"one\"\n          id = \"one\"\n          carrier = \"post\"\n", string.Empty, StringComparison.Ordinal));

    void Because() => Validate();

    [Fact] void should_refuse_the_specification() => _diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(["STAGE-ESM-011"]);
    [Fact] void should_say_why() => _diagnostics.Single().Message.ShouldContain("do not make the append violate constraint 'OneDispatch'");
}
