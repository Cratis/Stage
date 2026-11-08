// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageHostMode;

public class when_given_an_unknown_mode : given.a_host_configuration
{
    void Establish() => _settings["Stage:Mode"] = "production";
    void Because() => _error = Catch.Exception(() => Read());

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<StageHostModeRefused>();
    [Fact] void should_name_the_supported_modes() => _error!.Message.ShouldEqual("Stage:Mode must be Sandbox or Published.");
}
