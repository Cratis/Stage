// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_creating_the_version_4_backend_bytes : a_current_scaffold
{
    [Fact] void should_target_net11() => Content("Directory.Build.props").ShouldContain("net11.0");
    [Fact] void should_not_target_net10() => Content("Directory.Build.props").ShouldNotContain("net10.0");
    [Fact] void should_version_every_input_as_4() => _first.All(input => input.Version == "4").ShouldBeTrue();
    [Fact] void should_repeat_the_same_bytes() => _first.Zip(_second).All(pair => pair.First.Name == pair.Second.Name && pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
}
