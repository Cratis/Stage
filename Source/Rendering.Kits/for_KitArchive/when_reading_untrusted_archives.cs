// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Formats.Tar;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Kits.for_KitArchive;

public class when_reading_untrusted_archives : Specification
{
    [Theory]
    [InlineData("../escape", TarEntryType.RegularFile, KitCodes.Traversal)]
    [InlineData("/rooted", TarEntryType.RegularFile, KitCodes.RootedPath)]
    [InlineData("link", TarEntryType.SymbolicLink, KitCodes.SymbolicLink)]
    [InlineData("link", TarEntryType.HardLink, KitCodes.SymbolicLink)]
    [InlineData("directory", TarEntryType.Directory, KitCodes.SymbolicLink)]
    [InlineData("pipe", TarEntryType.Fifo, KitCodes.LoadLimit)]
    void should_refuse_unsafe_entries(string name, TarEntryType type, string code)
    {
        using var stream = new MemoryStream();
        using (var writer = new TarWriter(stream, TarEntryFormat.Ustar, true))
        {
            var entry = new UstarTarEntry(type, name);
            if (type is TarEntryType.SymbolicLink or TarEntryType.HardLink)
            {
                entry.LinkName = "target";
            }

            writer.WriteEntry(entry);
        }

        KitLoader.Load(KitArchive.Read([.. stream.ToArray()]), given.kit.Capabilities).Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);
    }

    [Fact] void should_refuse_malformed_archive_bytes() => KitLoader.Load(KitArchive.Read([1, 2, 3]), given.kit.Capabilities).Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(KitCodes.UnreadableSource);

    [Fact]
    void should_refuse_a_malformed_header_without_throwing()
    {
        var archive = KitArchive.Write(new KitFiles(new Dictionary<string, ImmutableArray<byte>> { ["file"] = [1, 2, 3] })).ToArray();

        // The ustar size field starts at offset 124; a non-octal digit makes the header invalid.
        archive[124] = (byte)'9';
        archive[125] = (byte)'z';

        KitLoader.Load(KitArchive.Read([.. archive]), given.kit.Capabilities).Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(KitCodes.UnreadableSource);
    }

    [Fact]
    void should_preserve_portable_long_ustar_paths()
    {
        var path = new string('a', 100) + "/" + new string('b', 100);
        var archive = KitArchive.Write(new KitFiles(new Dictionary<string, ImmutableArray<byte>> { [path] = [1, 2, 3] }));
        KitArchive.Read(archive).Read(path).AsEnumerable().ShouldEqual([1, 2, 3]);
    }
}
