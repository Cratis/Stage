// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_ModelAssets;

public class when_assets_root_is_a_link : given.an_asset_directory
{
    void Establish()
    {
        Directory.Delete(_assets);
        Directory.CreateDirectory(Path.Combine(_root, "outside"));
        File.WriteAllText(Path.Combine(_root, "outside", "file.json"), "outside");
        Directory.CreateSymbolicLink(_assets, Path.Combine(_root, "outside"));
    }

    void Because() => _result = ModelAssets.Read(_root);
    [Fact] void should_not_read_the_link_target() => _result.Files.ShouldBeEmpty();
}
#endif
