// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageHostMode;

public class when_published_with_warm_hosting_from_the_environment : given.a_host_configuration
{
    void Establish()
    {
        _arguments = ["--published"];
        _settings["STAGE_WARM"] = "true";
        _settings["Stage:Sandbox:ForwardedIdentity"] = "true";
    }

    void Because() => _error = Catch.Exception(() => Read());

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<StageHostModeRefused>();
    [Fact] void should_explain_warm_hosting_is_not_supported() => _error!.Message.ShouldContain("--warm or STAGE_WARM");
}
