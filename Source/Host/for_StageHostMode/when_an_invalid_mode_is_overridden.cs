// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Stage.Host.for_StageHostMode;

public class when_an_invalid_mode_is_overridden
{
    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("")]
    [InlineData("Staging")]
    void should_refuse_startup(string mode)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Stage:Mode"] = mode,
            ["Stage:Sandbox:ForwardedIdentity"] = "true"
        }).Build();
        Assert.Throws<StageHostModeRefused>(() => StageHostModeSelection.Read(configuration, ["--published"], StageRuntimeEngine.Semantic));
    }
}
