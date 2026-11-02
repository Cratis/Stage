// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageHostMode;

public class when_published_without_forwarded_identity : given.a_host_configuration
{
    void Establish() => _arguments = ["--published"];
    void Because() => _error = Catch.Exception(() => Read());

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<StageHostModeRefused>();
    [Fact] void should_name_the_required_identity_setting() => _error!.Message.ShouldContain("Stage:Sandbox:ForwardedIdentity=true");
}
