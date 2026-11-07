// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageHostMode;

public class when_defaulting_to_sandbox : given.a_host_configuration
{
    void Because() => _mode = Read();

    [Fact] void should_keep_sandbox_as_the_default() => _mode.ShouldEqual(StageHostMode.Sandbox);
}
