// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageHostMode;

public class when_requested_as_published : given.a_host_configuration
{
    void Establish()
    {
        _arguments = ["--published"];
        _settings["Stage:Mode"] = "Sandbox";
        _settings["Stage:Sandbox:ForwardedIdentity"] = "true";
    }

    void Because() => _mode = Read();

    [Fact] void should_override_the_configured_sandbox_mode() => _mode.ShouldEqual(StageHostMode.Published);
}
