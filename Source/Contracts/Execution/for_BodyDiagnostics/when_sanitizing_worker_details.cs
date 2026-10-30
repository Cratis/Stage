// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_BodyDiagnostics;

public class when_sanitizing_worker_details : Specification
{
    [Fact]
    void should_strip_paths_stack_frames_and_multiline_details()
    {
        var text = BodyDiagnostics.Format(BodyDiagnostics.WorkerFailed, "Failed /private/model/body.cs\r\n at Worker.Execute()");
        text.ShouldNotContain("/private");
        text.ShouldNotContain("Worker.Execute");
        text.ShouldNotContain("\n");
        BodyDiagnostics.Format(BodyDiagnostics.WorkerFailed, new string('x', 500)).Length.ShouldEqual(240);
    }
}
