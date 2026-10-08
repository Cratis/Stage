// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_running_all : a_specification_endpoint
{
    async Task Because() => await Request("{\"scopes\":[]}");

    [Fact] void should_return_ok() => _status.ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_return_a_passed_record() => Report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_return_the_semantic_report_schema() => Report.SchemaVersion.ShouldEqual("stage-spec-run/1");
    [Fact] void should_include_the_produced_fact() => Report.Results.Single().Trace!.Facts.Count.ShouldEqual(1);
}
