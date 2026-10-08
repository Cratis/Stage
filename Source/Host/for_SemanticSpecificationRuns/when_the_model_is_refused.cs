// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_the_model_is_refused : a_specification_endpoint
{
    protected override bool Refused => true;

    async Task Because() => await Request("{}");

    [Fact] void should_return_not_implemented() => _status.ShouldEqual(StatusCodes.Status501NotImplemented);
    [Fact] void should_carry_the_refusal() => _body.Contains("Unsupported(Plan)", StringComparison.Ordinal).ShouldBeTrue();
}
