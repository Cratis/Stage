// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_PublishedPackage;

public class when_loading_a_package_model : Export.for_StageExport.given.an_exportable_model
{
    string _name = null!;

    async Task Because()
    {
        var folder = await Export.StageExport.Run(Request(Output("pkg")) with { ApplicationName = "Shop" });
        var package = PublishedPackage.Read(folder);
        var (loaded, _) = await SemanticHost.LoadModel(package.ModelPath, [], null, package.Manifest.ApplicationName);
        _name = loaded!.Model.Application.Name;
    }

    [Fact] void should_name_the_application_as_the_export_did() => _name.ShouldEqual("Shop");
}
