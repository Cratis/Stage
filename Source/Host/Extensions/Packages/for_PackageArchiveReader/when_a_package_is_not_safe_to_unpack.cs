// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Formats.Tar;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_PackageArchiveReader;

public class when_a_package_is_not_safe_to_unpack : Specification
{
    PackageRefusedException _traversal = null!;
    PackageRefusedException _absolute = null!;
    PackageRefusedException _symlink = null!;
    PackageRefusedException _duplicate = null!;
    PackageRefusedException _outside = null!;
    PackageRefusedException _tooLarge = null!;
    PackageRefusedException _tooMany = null!;

    static PackageRefusedException Refused(PackageArchiveReader reader, byte[] archive) =>
        Assert.Throws<PackageRefusedException>(() => reader.Read([new MemoryStream(archive)], "extension"));

    void Because()
    {
        var reader = new PackageArchiveReader(1000, 3);
        var file = new byte[] { 1 };
        _traversal = Refused(reader, PackageFixtures.RawArchive(("extension/../evil", TarEntryType.RegularFile, file)));
        _absolute = Refused(reader, PackageFixtures.RawArchive(("/etc/evil", TarEntryType.RegularFile, file)));
        _symlink = Refused(reader, PackageFixtures.RawArchive(("extension/link", TarEntryType.SymbolicLink, file)));
        _duplicate = Refused(reader, PackageFixtures.RawArchive(("extension/a", TarEntryType.RegularFile, file), ("extension/a", TarEntryType.RegularFile, file)));
        _outside = Refused(reader, PackageFixtures.RawArchive(("other/a", TarEntryType.RegularFile, file)));
        _tooLarge = Refused(reader, PackageFixtures.RawArchive(("extension/a", TarEntryType.RegularFile, new byte[2000])));
        _tooMany = Refused(reader, PackageFixtures.RawArchive(("extension/a", TarEntryType.RegularFile, file), ("extension/b", TarEntryType.RegularFile, file), ("extension/c", TarEntryType.RegularFile, file), ("extension/d", TarEntryType.RegularFile, file)));
    }

    [Fact] void should_refuse_a_path_that_leaves_the_package() => _traversal.Reason.ShouldEqual(PackageRefusalReason.Malformed);
    [Fact] void should_refuse_an_absolute_path() => _absolute.Reason.ShouldEqual(PackageRefusalReason.Malformed);
    [Fact] void should_refuse_a_link() => _symlink.Message.ShouldContain("SymbolicLink");
    [Fact] void should_refuse_a_path_that_appears_twice() => _duplicate.Message.ShouldContain("more than once");
    [Fact] void should_refuse_a_file_outside_the_package_folder() => _outside.Message.ShouldContain("not under");
    [Fact] void should_refuse_a_package_that_unpacks_to_too_much() => _tooLarge.Reason.ShouldEqual(PackageRefusalReason.TooLarge);
    [Fact] void should_refuse_a_package_with_too_many_files() => _tooMany.Reason.ShouldEqual(PackageRefusalReason.TooLarge);
}
