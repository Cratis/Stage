// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_a_then_route : a_routed_plan
{
    SemanticEventRoute? _reportedRoute;

    async Task Because()
    {
        await Run(_plan);
        var report = new SemanticSpecificationRunReport("stage-spec-run/1", _plan.Model.Application.Id.ToString(), _plan.Revision.ToString(), [_result]);
        _reportedRoute = SemanticSpecificationRunReportFile.Read(SemanticSpecificationRunReportFile.Write(report))!.Results.Single().Trace!.Facts.Single().Route;
    }

    [Fact] void should_pass_like_the_reference() => AssertParity(true);
    [Fact] void should_report_the_stored_source_type() => _reportedRoute!.SourceKind.ShouldEqual("stored-account");
    [Fact] void should_report_the_stored_stream_type() => _reportedRoute!.StreamKind.ShouldEqual("stored-transactions");
    [Fact] void should_report_the_canonical_stream_id() => _reportedRoute!.StreamId.ShouldEqual("2026-10");
}
