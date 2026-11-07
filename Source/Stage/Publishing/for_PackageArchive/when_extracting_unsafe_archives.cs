// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Formats.Tar;
using System.IO.Compression;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Publishing.for_PackageArchive;

public class when_extracting_unsafe_archives : Specification
{
    string _folder = null!;

    void Establish()
    {
        _folder = Directory.CreateTempSubdirectory("stage-unsafe-spec").FullName;
        Directory.CreateDirectory(Path.Combine(_folder, "out"));
    }

    void Destroy() => Directory.Delete(_folder, recursive: true);

    [Fact] void should_refuse_an_entry_that_climbs_out() => Extract(Entry("../escape.txt")).Message.ShouldContain("not a relative path");
    [Fact] void should_refuse_an_absolute_entry() => Extract(Entry("/etc/escape.txt")).Message.ShouldContain("not a relative path");
    [Fact] void should_refuse_a_symbolic_link() => Extract(new UstarTarEntry(TarEntryType.SymbolicLink, "model/link") { LinkName = "/etc/passwd" }).Message.ShouldContain("SymbolicLink");
    [Fact] void should_refuse_a_hard_link() => Extract(new UstarTarEntry(TarEntryType.HardLink, "model/link") { LinkName = "manifest.json" }).Message.ShouldContain("HardLink");
    [Fact] void should_write_nothing_outside() => File.Exists(Path.Combine(_folder, "escape.txt")).ShouldBeFalse();

    static UstarTarEntry Entry(string name) => new(TarEntryType.RegularFile, name) { DataStream = new MemoryStream([1, 2, 3]) };

    InvalidDataException Extract(TarEntry entry)
    {
        var archive = Path.Combine(_folder, "bad.tar.gz");
        using (var file = File.Create(archive))
        using (var gzip = new GZipStream(file, CompressionLevel.Fastest))
        using (var tar = new TarWriter(gzip))
        {
            tar.WriteEntry(entry);
        }

        return Assert.Throws<InvalidDataException>(() => PackageArchive.Extract(archive, Path.Combine(_folder, "out")));
    }
}
