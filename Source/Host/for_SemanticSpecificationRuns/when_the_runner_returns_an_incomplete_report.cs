// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_the_runner_returns_an_incomplete_report : when_the_runner_fails
{
    protected override int ExitCode => 0;
    protected override string ReportContent => "{}";
}
