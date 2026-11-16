// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_ModelAssets;

public class when_reading_visible_binary_assets : given.an_asset_directory
{
    void Establish()
    {
        Write("nested/picture.png", [0, 255, 128, 10, 13]);
        Write("logo.svg", [60, 115, 118, 103, 47, 62]);
        Write("source.PLAY", [1]);
        Write(".hidden.svg", [1]);
        Write(".hidden/nested.svg", [1]);
        File.WriteAllText(Path.Combine(_root, "secret.json"), "not an asset");
    }

    void Because() => _result = ModelAssets.Read(_root);

    [Fact] void should_sort_visible_assets() => _result.Files.Keys.ShouldEqual(["logo.svg", "nested/picture.png"]);
    [Fact] void should_preserve_binary_bytes() => _result.Files["nested/picture.png"].ShouldEqual(new byte[] { 0, 255, 128, 10, 13 });
    [Fact] void should_have_no_warnings() => _result.Diagnostics.ShouldBeEmpty();
}
#endif
