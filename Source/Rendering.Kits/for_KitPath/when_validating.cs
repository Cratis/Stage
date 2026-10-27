// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Kits.for_KitPath;

public class when_validating : Specification
{
    [Theory]
    [InlineData("a/../b", KitCodes.Traversal)]
    [InlineData("a/./b", KitCodes.Traversal)]
    [InlineData("a//b", KitCodes.Traversal)]
    [InlineData("", KitCodes.Traversal)]
    [InlineData("/a", KitCodes.RootedPath)]
    [InlineData("C:/a", KitCodes.RootedPath)]
    [InlineData("\\a", KitCodes.RootedPath)]
    [InlineData("a\\b", KitCodes.NonPortablePath)]
    [InlineData("café.sbn", KitCodes.NonPortablePath)]
    [InlineData("a b", KitCodes.NonPortablePath)]
    [InlineData("templates/a.sbn", null)]
    void should_validate_portable_relative_paths(string path, string? code) => KitPath.Validate(path).ShouldEqual(code);

    [Fact] void should_refuse_paths_over_the_archive_limit() => KitPath.Validate(new string('a', 256)).ShouldEqual(KitCodes.NonPortablePath);
}
