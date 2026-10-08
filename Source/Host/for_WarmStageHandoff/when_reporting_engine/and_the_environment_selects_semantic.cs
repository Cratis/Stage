// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_WarmStageHandoff.when_reporting_engine;

public class and_the_environment_selects_semantic : given.a_runtime_engine_environment
{
    StageStatus _status = null!;

    void Establish() => Environment.SetEnvironmentVariable("Stage__Runtime__Engine", " Semantic ");

    void Because() => _status = _handoff.GetStatus();

    [Fact] void should_report_the_normalized_engine() => _status.Engine.ShouldEqual("semantic");
}
