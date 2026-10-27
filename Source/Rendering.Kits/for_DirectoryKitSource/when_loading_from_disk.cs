// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Kits.for_DirectoryKitSource;

public class when_loading_from_disk : Specification
{
    string _root = string.Empty;

    void Establish()
    {
        _root = Directory.CreateTempSubdirectory("stage-kit-specs-").FullName;
        Directory.CreateDirectory(Path.Combine(_root, "templates"));
        Directory.CreateDirectory(Path.Combine(_root, "templates"));
        foreach (var file in given.kit.Files())
        {
            File.WriteAllBytes(Path.Combine(_root, file.Key), [.. file.Value]);
        }
    }

    void Destroy() => Directory.Delete(_root, true);

    [Fact] void should_match_the_memory_source() => KitLoader.Load(new DirectoryKitSource(_root), given.kit.Capabilities).Kit!.Digest.ShouldEqual(given.kit.Load(given.kit.Manifest).Kit!.Digest);

    [Theory]
    [InlineData("file")]
    [InlineData("directory")]
    [InlineData("root")]
    [InlineData("ancestor")]
    void should_refuse_links_at_every_level(string kind)
    {
        var link = Path.Combine(_root, "link");
        var root = _root;
        switch (kind)
        {
            case "file":
                File.CreateSymbolicLink(link, Path.Combine(_root, "templates/main.sbn"));
                break;
            case "directory":
                Directory.CreateSymbolicLink(link, Path.Combine(_root, "templates"));
                break;
            case "root":
                Directory.CreateSymbolicLink(link, _root);
                root = link;
                break;
            case "ancestor":
                Directory.CreateDirectory(Path.Combine(_root, "nested"));
                Directory.CreateSymbolicLink(Path.Combine(_root, "nested", "link"), Path.Combine(_root, "templates"));
                break;
        }

        KitLoader.Load(new DirectoryKitSource(root), given.kit.Capabilities).Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(KitCodes.SymbolicLink);
    }

    [Fact]
    void should_refuse_reading_through_a_linked_directory_below_the_root()
    {
        Directory.CreateSymbolicLink(Path.Combine(_root, "link"), Path.Combine(_root, "templates"));
        (Catch.Exception(() => new DirectoryKitSource(_root).Read("link/main.sbn")) as KitSourceException)!.Code.ShouldEqual(KitCodes.SymbolicLink);
    }
}
