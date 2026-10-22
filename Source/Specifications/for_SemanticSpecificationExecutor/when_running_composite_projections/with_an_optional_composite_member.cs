// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_projections;

public class with_an_optional_composite_member : given.a_composite_projection
{
    Task Because() => Run(addressType: "type Address\n  street String\n  city String\n  note String optional");
    [Fact] void should_be_unsupported() => AssertOutcome(SemanticSpecificationOutcome.Unsupported);
}
