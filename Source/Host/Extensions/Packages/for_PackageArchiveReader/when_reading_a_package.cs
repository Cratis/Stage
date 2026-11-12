// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_PackageArchiveReader;

public class when_reading_a_package : Specification
{
    PackageContents _gzipped = null!;
    PackageContents _plain = null!;

    void Because()
    {
        var files = PackageFixtures.Files();
        files["backend/server.dll"] = new byte[1000];
        var reader = new PackageArchiveReader(1_000_000, 100);
        _gzipped = reader.Read([new MemoryStream(PackageFixtures.Archive("extension", files))], "extension");
        _plain = reader.Read([new MemoryStream(PackageFixtures.Archive("extension", files, gzip: false))], "extension");
    }

    [Fact] void should_hold_the_contract() => Encoding.UTF8.GetString(_gzipped.Contract!).ShouldContain("extension Mail");
    [Fact] void should_hold_the_frontend() => _gzipped.Find("frontend/index.js").ShouldNotBeNull();
    [Fact] void should_read_an_archive_that_is_not_compressed_too() => _plain.Files.Keys.ShouldContain("contract.play");
    [Fact] void should_only_list_the_backend() => _gzipped.Backend.Single().ShouldEqual(new PackageFileInfo("backend/server.dll", 1000));
    [Fact] void should_not_hold_the_backend_in_memory() => _gzipped.Files.Keys.ShouldNotContain("backend/server.dll");
}
