// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_ModelAssets;

public class when_assets_contain_links : given.an_asset_directory
{
    void Establish()
    {
        Write("visible.svg", [1, 2]);
        File.WriteAllText(Path.Combine(_root, "outside.json"), "outside");
        Directory.CreateDirectory(Path.Combine(_root, "outside"));
        File.WriteAllText(Path.Combine(_root, "outside", "file.json"), "outside");
        File.CreateSymbolicLink(Path.Combine(_assets, "escape.json"), Path.Combine(_root, "outside.json"));
        Directory.CreateSymbolicLink(Path.Combine(_assets, "escape"), Path.Combine(_root, "outside"));
        File.CreateSymbolicLink(Path.Combine(_assets, "inside.svg"), Path.Combine(_assets, "visible.svg"));
        Directory.CreateSymbolicLink(Path.Combine(_assets, "cycle"), _assets);
    }

    void Because() => _result = ModelAssets.Read(_root);
    [Fact] void should_never_follow_links() => _result.Files.Keys.ShouldEqual(["visible.svg"]);
}
#endif
