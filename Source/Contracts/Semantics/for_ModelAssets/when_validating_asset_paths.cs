// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_ModelAssets;

public class when_validating_asset_paths : Specification
{
    [Theory]
    [InlineData("")]
    [InlineData("/root.svg")]
    [InlineData("../secret.json")]
    [InlineData("nested/../secret.json")]
    [InlineData("nested//file.svg")]
    [InlineData("nested/.hidden/file.svg")]
    [InlineData("C:/file.svg")]
    [InlineData("nested\\file.svg")]
    [InlineData("line\nfeed.svg")]
    void should_reject_unsafe_paths(string path) => ModelAssets.IsSafePath(path).ShouldBeFalse();
}
#endif
