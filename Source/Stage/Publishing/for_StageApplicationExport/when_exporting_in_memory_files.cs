// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Publishing.for_StageApplicationExport;

public class when_exporting_in_memory_files : given.an_exportable_model
{
    StageApplicationPackage _package = null!;
    StageApplicationPackage _second = null!;

    void Because()
    {
        var references = new Dictionary<string, string> { ["module:Shop"] = "studio-module-1", ["slice:Shop.Orders.PlaceOrder"] = "studio-slice-9" };
        _package = StageApplicationExport.Export(Request() with { ModelReferences = references });
        _second = StageApplicationExport.Export(Request() with { ModelReferences = references });
    }

    [Fact] void should_name_the_application_explicitly() => _package.Manifest.ApplicationName.ShouldEqual("Shop");
    [Fact] void should_name_the_published_application() => _package.Manifest.PublishedName.ShouldEqual("my-shop");
    [Fact] void should_carry_the_application_version() => _package.Manifest.ApplicationVersion.ShouldEqual("1.2.0");
    [Fact] void should_require_this_runtime() => _package.Manifest.StageRuntimeVersion.ShouldEqual(StageRuntimeVersion.Current);
    [Fact] void should_require_sign_in() => _package.Manifest.Authentication.Required.ShouldBeTrue();
    [Fact] void should_list_the_roles() => _package.Manifest.Authentication.Roles.ShouldContainOnly("Member");
    [Fact] void should_map_the_references() => _package.Manifest.ModelReferences.Select(reference => $"{reference.Kind}:{reference.Name}={reference.Id}").ShouldContainOnly("module:Shop=studio-module-1", "slice:Shop.Orders.PlaceOrder=studio-slice-9");
    [Fact] void should_hold_the_manifest_the_json_describes() => PackageManifestJson.Write(PackageManifestJson.Read(_package.ManifestJson)).ShouldEqual(_package.ManifestJson);
    [Fact] void should_carry_the_manifest_as_a_file() => Encoding.UTF8.GetString(_package.Files["manifest.json"]).ShouldEqual(_package.ManifestJson);
    [Fact] void should_carry_the_model_files() => Encoding.UTF8.GetString(_package.Files["model/shop.play"]).ShouldEqual(Secured);
    [Fact] void should_carry_a_dockerfile() => Encoding.UTF8.GetString(_package.Files["Dockerfile"]).ShouldContain($"FROM cratis/stage:{StageRuntimeVersion.Current}");
    [Fact] void should_digest_the_model_files() => _package.Manifest.ModelDigest.ShouldEqual(PackageDigest.Of(_package.Files.Where(file => file.Key.StartsWith("model/", StringComparison.Ordinal)).Select(file => new KeyValuePair<string, byte[]>(file.Key["model/".Length..], file.Value))));
    [Fact] void should_give_identical_manifests_for_identical_input() => _second.ManifestJson.ShouldEqual(_package.ManifestJson);
}
