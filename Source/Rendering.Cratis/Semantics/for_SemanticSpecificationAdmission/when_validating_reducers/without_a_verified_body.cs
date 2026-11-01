// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reducers;

public class without_a_verified_body : given.a_reducer_specification
{
    void Establish() => _request = _request with { ImplementationContents = _request.ImplementationContents.Clear() };
    void Because() => Validate();
    [Fact] void should_include_the_body_diagnostic() => Refusal().ShouldContain("has no resolved body");
}
