// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Publishing.for_StageApplicationExport;

public class when_the_request_is_refused : given.an_exportable_model
{
    [Fact] void should_refuse_a_version_that_is_not_semantic() => Assert.Throws<StageExportRefused>(() => StageApplicationExport.Export(Request() with { ApplicationVersion = "latest" })).Message.ShouldContain("semantic version");
    [Fact] void should_refuse_a_name_that_is_not_a_host_label() => Assert.Throws<StageExportRefused>(() => StageApplicationExport.Export(Request() with { PublishedName = "My Shop" })).Message.ShouldContain("host label");
    [Fact] void should_refuse_a_missing_application_name() => Assert.Throws<StageExportRefused>(() => StageApplicationExport.Export(Request() with { ApplicationName = " " })).Reasons.ShouldContain("The application name is required.");
    [Fact] void should_refuse_a_reference_to_an_element_the_model_lacks() => Assert.Throws<StageExportRefused>(() => StageApplicationExport.Export(Request() with { ModelReferences = new Dictionary<string, string> { ["module:Nope"] = "x" } })).Message.ShouldContain("module:Nope");
    [Fact] void should_refuse_a_model_that_does_not_compile() => Assert.Throws<StageExportRefused>(() => StageApplicationExport.Export(Request("this is not screenplay")));
    [Fact] void should_refuse_a_model_without_authentication() => Assert.Throws<StageExportRefused>(() => StageApplicationExport.Export(Request(Open))).Message.ShouldContain("no authentication policy");
    [Fact] void should_refuse_a_path_that_leaves_the_model() => Assert.Throws<StageExportRefused>(() => StageApplicationExport.Export(new(new Dictionary<string, string> { ["../shop.play"] = Secured }, "Shop", "1.2.0", "my-shop"))).Message.ShouldContain("relative path");
    [Fact] void should_refuse_a_model_without_files() => Assert.Throws<StageExportRefused>(() => StageApplicationExport.Export(new(new Dictionary<string, string>(), "Shop", "1.2.0", "my-shop"))).Message.ShouldContain("no files");
}
