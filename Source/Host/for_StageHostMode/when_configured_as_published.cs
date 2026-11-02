// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageHostMode;

public class when_configured_as_published : given.a_host_configuration
{
    void Establish()
    {
        _settings["Stage:Mode"] = "pUbLiShEd";
        _settings["Stage:Sandbox:ForwardedIdentity"] = "true";
    }

    void Because() => _mode = Read();

    [Fact] void should_accept_the_mode_case_insensitively() => _mode.ShouldEqual(StageHostMode.Published);
}
