// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_StageHostModeSelection;

public class when_selecting
{
    static IConfiguration Configuration(string? mode) => new ConfigurationBuilder().AddInMemoryCollection(
        mode is null ? [] : new Dictionary<string, string?> { ["Stage:Mode"] = mode }).Build();

    [Fact] void should_default_to_sandbox() => StageHostModeSelection.Read([], Configuration(null)).ShouldEqual(StageHostMode.Sandbox);
    [Fact] void should_select_published_by_argument() => StageHostModeSelection.Read(["--published"], Configuration(null)).ShouldEqual(StageHostMode.Published);
    [Fact] void should_select_published_by_configuration() => StageHostModeSelection.Read([], Configuration("Published")).ShouldEqual(StageHostMode.Published);
    [Fact] void should_ignore_case() => StageHostModeSelection.Read([], Configuration("published")).ShouldEqual(StageHostMode.Published);
    [Fact] void should_select_sandbox_explicitly() => StageHostModeSelection.Read([], Configuration("Sandbox")).ShouldEqual(StageHostMode.Sandbox);
    [Fact] void should_refuse_an_unknown_mode() => Assert.Throws<PublishedModeRefused>(() => StageHostModeSelection.Read([], Configuration("Staging")));
}
