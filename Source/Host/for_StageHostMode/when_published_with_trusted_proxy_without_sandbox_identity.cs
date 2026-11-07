// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageHostMode;

public class when_published_with_trusted_proxy_without_sandbox_identity : given.a_host_configuration
{
    void Establish()
    {
        _arguments = ["--published"];
        _settings["Stage:Published:Storage:ConnectionString"] = "mongodb://storage:27017";
        _settings["Stage:Published:Authentication:Mode"] = "TrustedProxy";
        _settings["Stage:Published:TrustedProxies:0"] = "127.0.0.1";
        _settings["AllowedHosts"] = "app.example.com";
    }

    void Because() => _error = Catch.Exception(() => _mode = Read());

    [Fact] void should_accept_the_published_authentication() => _error.ShouldBeNull();
    [Fact] void should_select_published() => _mode.ShouldEqual(StageHostMode.Published);
}
