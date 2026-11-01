// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Export.for_StageExport;

public class when_the_request_is_invalid : given.an_exportable_model
{
    [Fact] async Task should_refuse_a_version_that_is_not_semantic() => (await Assert.ThrowsAsync<StageExportRefused>(() => StageExport.Run(Request(Output("a")) with { ApplicationVersion = "latest" }))).Message.ShouldContain("semantic version");
    [Fact] async Task should_refuse_a_name_that_is_not_a_host_label() => (await Assert.ThrowsAsync<StageExportRefused>(() => StageExport.Run(Request(Output("b")) with { PublishedName = "My Shop" }))).Message.ShouldContain("host label");
    [Fact] async Task should_refuse_a_reference_to_an_element_the_model_lacks() => (await Assert.ThrowsAsync<StageExportRefused>(() => StageExport.Run(Request(Output("c")) with { ModelReferences = new Dictionary<string, string> { ["module:Nope"] = "x" } }))).Message.ShouldContain("module:Nope");

    [Fact]
    async Task should_refuse_a_model_that_does_not_compile()
    {
        await File.WriteAllTextAsync(Path.Combine(_model, "shop.play"), "this is not screenplay");
        await Assert.ThrowsAsync<StageExportRefused>(() => StageExport.Run(Request(Output("d"))));
    }

    [Fact]
    async Task should_refuse_a_folder_that_is_not_empty()
    {
        Directory.CreateDirectory(Output("e"));
        await File.WriteAllTextAsync(Path.Combine(Output("e"), "keep.txt"), "x");
        (await Assert.ThrowsAsync<StageExportRefused>(() => StageExport.Run(Request(Output("e"))))).Message.ShouldContain("not empty");
    }
}
