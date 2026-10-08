// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_WarmStageHandoff.when_reporting_engine;

public class and_no_engine_is_configured : given.a_runtime_engine_environment
{
    StageStatus _status = null!;

    void Because() => _status = _handoff.GetStatus();

    [Fact] void should_report_the_default_engine() => _status.Engine.ShouldEqual("eventmodel");
    [Fact] void should_keep_the_warm_state() => _status.State.ShouldEqual("warm");
}
