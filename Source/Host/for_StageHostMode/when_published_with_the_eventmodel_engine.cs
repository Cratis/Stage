// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageHostMode;

public class when_published_with_the_eventmodel_engine : given.a_host_configuration
{
    void Establish()
    {
        _arguments = ["--published"];
        _engine = StageRuntimeEngine.EventModel;
        _settings["Stage:Sandbox:ForwardedIdentity"] = "true";
    }

    void Because() => _error = Catch.Exception(() => Read());

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<StageHostModeRefused>();
    [Fact] void should_explain_the_engine_is_not_supported() => _error!.Message.ShouldContain("--engine=eventmodel");
}
