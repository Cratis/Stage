// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Publishing.for_StageApplicationExport;

public class when_archiving_a_package : given.an_exportable_model
{
    byte[] _first = null!;
    byte[] _second = null!;
    string _folder = null!;

    void Because()
    {
        _first = StageApplicationExport.Export(Request()).ToArchive();
        _second = StageApplicationExport.Export(Request()).ToArchive();
        _folder = Directory.CreateTempSubdirectory("stage-archive-spec").FullName;
        var file = Path.Combine(_folder, "package.tar.gz");
        File.WriteAllBytes(file, _first);
        Directory.CreateDirectory(Path.Combine(_folder, "out"));
        PackageArchive.Extract(file, Path.Combine(_folder, "out"));
    }

    void Destroy() => Directory.Delete(_folder, recursive: true);

    [Fact] void should_be_gzip() => (_first[0] == 0x1f && _first[1] == 0x8b).ShouldBeTrue();
    [Fact] void should_give_the_same_bytes_for_the_same_input() => _second.ShouldEqual(_first);
    [Fact] void should_extract_the_manifest() => File.ReadAllText(Path.Combine(_folder, "out", "manifest.json")).ShouldEqual(StageApplicationExport.Export(Request()).ManifestJson);
    [Fact] void should_extract_the_model() => File.ReadAllText(Path.Combine(_folder, "out", "model", "shop.play")).ShouldEqual(Secured);
    [Fact] void should_extract_the_dockerfile() => File.Exists(Path.Combine(_folder, "out", "Dockerfile")).ShouldBeTrue();
}
