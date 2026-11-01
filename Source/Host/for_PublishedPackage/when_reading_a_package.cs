// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_PublishedPackage;

public class when_reading_a_package : Export.for_StageExport.given.an_exportable_model
{
    string _folder = null!;

    async Task Establish() => _folder = await Export.StageExport.Run(Request(Output("pkg")));

    [Fact] void should_refuse_a_missing_package() => Assert.Throws<PublishedModeRefused>(() => PublishedPackage.Read(Output("nowhere"))).Message.ShouldContain("No application package");

    [Fact]
    void should_refuse_a_model_that_was_changed_after_export()
    {
        File.AppendAllText(Path.Combine(_folder, "model", "shop.play"), "\n// edited");
        Assert.Throws<PublishedModeRefused>(() => PublishedPackage.Read(_folder)).Message.ShouldContain("digest");
    }

    [Fact]
    void should_refuse_a_package_for_another_runtime()
    {
        var path = Path.Combine(_folder, "manifest.json");
        File.WriteAllText(path, File.ReadAllText(path).Replace($"\"stageRuntimeVersion\": \"{StageRuntimeVersion.Current}\"", "\"stageRuntimeVersion\": \"0.0.1-other\""));
        Assert.Throws<PublishedModeRefused>(() => PublishedPackage.Read(_folder)).Message.ShouldContain("requires Stage runtime 0.0.1-other");
    }
}
