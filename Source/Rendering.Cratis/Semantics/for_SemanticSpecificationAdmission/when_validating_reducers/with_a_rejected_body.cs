// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reducers;

public class with_a_rejected_body : given.a_reducer_specification
{
    protected override string Body => "return new Total(Guid.NewGuid(), context.Event.Amount);";
    void Because() => Validate();
    [Fact] void should_include_the_analysis_verdict()
    {
        Refusal().ShouldContain("STAGE-ESM-022");
        Refusal().ShouldContain("NewGuid");
    }
}
