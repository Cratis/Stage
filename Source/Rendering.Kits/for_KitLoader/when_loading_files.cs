// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Kits.for_KitLoader;

public class when_loading_files : Specification
{
    [Theory]
    [InlineData("a/../b", KitCodes.Traversal)]
    [InlineData("/a", KitCodes.RootedPath)]
    [InlineData("a b", KitCodes.NonPortablePath)]
    [InlineData("TEMPLATES/main.sbn", KitCodes.DuplicatePath)]
    void should_reject_unsafe_source_paths(string path, string code)
    {
        var files = given.kit.Files();
        files.Add(path, []);
        KitLoader.Load(new InMemoryKitSource(files), given.kit.Capabilities).Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);
    }

    [Theory]
    [InlineData(KitSourceEntryKind.SymbolicLink, KitCodes.SymbolicLink)]
    [InlineData(KitSourceEntryKind.Other, KitCodes.LoadLimit)]
    void should_reject_unsafe_entry_kinds(KitSourceEntryKind kind, string code)
    {
        var source = Substitute.For<IKitSource>();
        source.Enumerate().Returns([new KitSourceEntry("unsafe", kind, 0)]);
        KitLoader.Load(source, given.kit.Capabilities).Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);
    }

    [Theory]
    [InlineData(1, 8388608, 67108864)]
    [InlineData(2048, 1, 67108864)]
    [InlineData(2048, 8388608, 1)]
    void should_enforce_each_load_limit(int count, long size, long total) => KitLoader.Load(new InMemoryKitSource(given.kit.Files()), given.kit.Capabilities, new(count, size, total)).Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(KitCodes.LoadLimit);

    [Theory]
    [InlineData("ff")]
    [InlineData("c0af")]
    [InlineData("eda080")]
    [InlineData("efbbbf6869")]
    void should_refuse_noncanonical_template_utf8(string hex)
    {
        var files = given.kit.Files();
        files["templates/main.sbn"] = [.. Convert.FromHexString(hex)];
        KitLoader.Load(new InMemoryKitSource(files), given.kit.Capabilities).Diagnostics.Single().Code.ShouldEqual(KitCodes.InvalidUtf8);
    }

    [Fact]
    void should_refuse_a_missing_manifest() => KitLoader.Load(new InMemoryKitSource([]), given.kit.Capabilities).Diagnostics.Single().Code.ShouldEqual(KitCodes.MissingManifest);

    [Fact]
    void should_refuse_a_missing_declared_file()
    {
        var files = given.kit.Files();
        files.Remove("templates/main.sbn");
        KitLoader.Load(new InMemoryKitSource(files), given.kit.Capabilities).Diagnostics.Single().Code.ShouldEqual(KitCodes.MissingFile);
    }

    [Fact]
    void should_refuse_undeclared_files()
    {
        var files = given.kit.Files();
        files.Add("extra", []);
        KitLoader.Load(new InMemoryKitSource(files), given.kit.Capabilities).Diagnostics.Single().Code.ShouldEqual(KitCodes.UndeclaredFile);
    }

    [Fact]
    void should_accept_binary_assets()
    {
        var files = given.kit.Files(given.kit.Manifest.Replace("\"assets\":[]", "\"assets\":[{\"path\":\"image.bin\",\"output\":\"image.bin\"}]", StringComparison.Ordinal));
        files.Add("image.bin", [0xff, 0x00]);
        KitLoader.Load(new InMemoryKitSource(files), given.kit.Capabilities).Succeeded.ShouldBeTrue();
    }

    [Fact]
    void should_refuse_noncanonical_table_utf8()
    {
        var files = given.kit.Files(given.kit.Manifest.Replace("\"tables\":[]", "\"tables\":[{\"name\":\"types\",\"path\":\"table.json\"}]", StringComparison.Ordinal));
        files.Add("table.json", [0xff]);
        KitLoader.Load(new InMemoryKitSource(files), given.kit.Capabilities).Diagnostics.Single().Code.ShouldEqual(KitCodes.InvalidUtf8);
    }
}
