// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Formats.Tar;
using System.IO.Compression;
using Cratis.Specifications;
using Cratis.Stage.Publishing;
using Xunit;

namespace Cratis.Stage.Host.Published.for_PublishedPackage;

public class when_reading_an_archive : Export.for_StageExport.given.an_exportable_model
{
    StageApplicationPackage _exported = null!;
    string _archive = null!;
    PublishedPackage _package = null!;

    async Task Establish()
    {
        var folder = await Export.StageExport.Run(Request(Output("pkg")));
        _exported = await StageApplicationExport.ExportAsync(new StageApplicationExportRequest(Export.StageExport.ReadModel(folder + "/model"), "model", "1.2.0", "my-shop"));
        _archive = Output("package.tar.gz");
        await File.WriteAllBytesAsync(_archive, _exported.ToArchive());
    }

    void Because() => _package = PublishedPackage.Read(_archive);

    [Fact] void should_read_the_manifest() => _package.Manifest.PublishedName.ShouldEqual("my-shop");
    [Fact] void should_load_the_model_from_the_extracted_folder() => File.Exists(Path.Combine(_package.ModelPath, "shop.play")).ShouldBeTrue();
    [Fact] void should_verify_the_digest_the_export_computed() => _package.Manifest.ModelDigest.ShouldEqual(_exported.Manifest.ModelDigest);

    [Fact]
    void should_refuse_an_archive_whose_model_was_changed()
    {
        var bad = Output("changed.tar.gz");
        var files = _exported.Files.ToDictionary(file => file.Key, file => file.Value);
        files["model/shop.play"] = [.. files["model/shop.play"], .. "\n// edited"u8.ToArray()];
        File.WriteAllBytes(bad, StageArchive(files));
        Assert.Throws<PublishedModeRefused>(() => PublishedPackage.Read(bad)).Message.ShouldContain("digest");
    }

    [Fact]
    void should_refuse_an_archive_that_escapes_the_package()
    {
        var bad = Output("escape.tar.gz");
        File.WriteAllBytes(bad, StageArchive(new Dictionary<string, byte[]> { ["../escape.txt"] = [1] }));
        Assert.Throws<PublishedModeRefused>(() => PublishedPackage.Read(bad)).Message.ShouldContain("not usable");
    }

    [Fact]
    void should_refuse_an_archive_holding_a_symbolic_link()
    {
        var bad = Output("link.tar.gz");
        using (var file = File.Create(bad))
        using (var gzip = new GZipStream(file, CompressionLevel.Fastest))
        using (var tar = new TarWriter(gzip))
        {
            tar.WriteEntry(new UstarTarEntry(TarEntryType.SymbolicLink, "model") { LinkName = "/etc" });
        }

        Assert.Throws<PublishedModeRefused>(() => PublishedPackage.Read(bad)).Message.ShouldContain("SymbolicLink");
    }

    static byte[] StageArchive(Dictionary<string, byte[]> files)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        using (var tar = new TarWriter(gzip))
        {
            foreach (var file in files)
            {
                tar.WriteEntry(new UstarTarEntry(TarEntryType.RegularFile, file.Key) { DataStream = new MemoryStream(file.Value) });
            }
        }

        return output.ToArray();
    }
}
