// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Kits.for_ContractVersion;

public class when_parsing : Specification
{
    [Theory]
    [InlineData("stage-target/v1.0", true)]
    [InlineData("render-model/v2.14", true)]
    [InlineData("stage-target/v1", false)]
    [InlineData("stage-target/v01.0", false)]
    [InlineData("stage-target/v2147483648.0", false)]
    [InlineData("stage-target/1.0", false)]
    void should_accept_only_named_major_minor_contracts(string text, bool accepted) => ContractVersion.TryParse(text, out _).ShouldEqual(accepted);

    [Fact] void should_preserve_the_identity() => new ContractVersion("stage-target", 1, 2).ToString().ShouldEqual("stage-target/v1.2");
}
