// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_running_an_unknown_scope : a_specification_endpoint
{
    const string Scope = "sem1:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    async Task Because() => await Request($$"""{"scopes":["{{Scope}}"]}""");

    [Fact] void should_return_ok() => _status.ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_return_unsupported() => Report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_identify_the_specification_capability() => Report.Results.Single().Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Specification);
    [Fact] void should_preserve_the_unknown_scope() => Report.Results.Single().Unsupported!.Construct.ShouldEqual(Scope);
}
