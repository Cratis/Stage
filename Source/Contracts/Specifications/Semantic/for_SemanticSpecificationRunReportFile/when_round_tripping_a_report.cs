// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Specifications.Semantic.for_SemanticSpecificationRunReportFile;

public class when_round_tripping_a_report : Cratis.Specifications.Specification
{
    SemanticSpecificationRunReport _report = null!;
    SemanticSpecificationRunReport _restored = null!;

    void Establish() => _report = new(
        "stage-spec-run/1",
        "sem1:application",
        "rev1:model",
        [new(
            "sem1:specification",
            "Accept",
            "sem1:slice",
            "StateChange",
            SemanticSpecificationOutcome.Unsupported,
            null,
            new(StageExecutionCapability.Projection, "sem1:readmodel", "Projection is not available."),
            [],
            null)]);

    void Because() => _restored = SemanticSpecificationRunReportFile.Read(SemanticSpecificationRunReportFile.Write(_report))!;

    [Fact] void should_preserve_the_schema() => _restored.SchemaVersion.ShouldEqual("stage-spec-run/1");
    [Fact] void should_preserve_the_unsupported_capability() => _restored.Results.Single().Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
    [Fact] void should_preserve_the_identity() => _restored.Results.Single().SpecificationId.ShouldEqual("sem1:specification");
    [Fact] void should_report_a_completed_unsupported_run() => _restored.Completed.ShouldBeTrue();
}
#endif
